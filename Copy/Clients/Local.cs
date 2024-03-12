using Copy.Types;
using System.Runtime.Versioning;
using System.Security.Principal;
using System.Text.RegularExpressions;

namespace Copy.Clients
{
    /// <summary>
    /// Local file system client.
    /// </summary>
    internal class Local : IClient
    {
        /// <summary>
        /// Client credentials.
        /// </summary>
        private readonly Client _credentials;

        public Client Config => _credentials;

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="credentials"></param>
        public Local(Client credentials)
        {
            _credentials = credentials;
        }

        public bool DoFileExist(string path)
        {
            return File.Exists(path);
        }

        [SupportedOSPlatform("windows")]
        public string[] ListFiles(string path, CopyFilter filter)
        {
            if (!Directory.Exists(path))
            {
                Logger.Error($"Directory {path} does not exist");
                throw new DirectoryNotFoundException($"Directory {path} does not exist");
            }

            Regex nameRegex = new(filter.Name);
            Regex authorRegex = new(filter.Author);

            return Directory.GetFiles(path)
                .Where(f =>
                {
                    FileInfo fileInfo = new(f);
                    bool authorMatch = authorRegex.IsMatch(fileInfo.GetAccessControl().GetOwner(typeof(NTAccount))?.Value ?? throw new FileOwnerNotFoundException($"Impossible to get owner of {f}"));
                    return nameRegex.IsMatch(Path.GetFileName(f)) && authorMatch && File.GetCreationTime(f) >= filter.CreatedAfter && (ulong)fileInfo.Length <= filter.MaxSize && (ulong)fileInfo.Length >= filter.MinSize;
                })
                .ToArray();
        }

        public Stream GetFile(string path)
        {
            string directory = Path.GetDirectoryName(path) ?? throw new ArgumentNullException($"Impossible to get directory from {path}");
            if (!Directory.Exists(directory))
            {
                Logger.Error($"Directory {directory} does not exist");
                throw new DirectoryNotFoundException($"Directory {directory} does not exist");
            }
            if (!DoFileExist(path))
            {
                Logger.Error($"File {path} does not exist");
                throw new FileNotFoundException($"File {path} does not exist");
            }

            return File.OpenRead(path);
        }

        public void PutFile(string path, Stream stream)
        {
            string directory = Path.GetDirectoryName(path) ?? throw new ArgumentNullException($"Impossible to get directory from {path}");
            if (!Directory.Exists(directory))
            {
                Logger.Warn($"Directory {directory} does not exist, creating");
                Directory.CreateDirectory(directory);
            }
            if (DoFileExist(path)) Logger.Warn($"File {path} already exists, overwriting");

            using var fileStream = File.Create(path);
            stream.CopyTo(fileStream);
        }

        public void MoveFile(string sourcePath, string destinationPath)
        {
            string directory = Path.GetDirectoryName(destinationPath) ?? throw new ArgumentNullException($"Impossible to get directory from {destinationPath}");
            if (!Directory.Exists(directory))
            {
                Logger.Warn($"Directory {directory} does not exist, creating");
                Directory.CreateDirectory(directory);
            }
            if (!DoFileExist(sourcePath))
            {
                Logger.Error($"File {sourcePath} does not exist");
                throw new FileNotFoundException($"File {sourcePath} does not exist");
            }
            if (DoFileExist(destinationPath)) Logger.Warn($"File {destinationPath} already exists, overwriting");

            File.Move(sourcePath, destinationPath);
        }

        public void CopyFile(string sourcePath, string destinationPath)
        {
            string directory = Path.GetDirectoryName(destinationPath) ?? throw new ArgumentNullException($"Impossible to get directory from {destinationPath}");
            if (!Directory.Exists(directory))
            {
                Logger.Warn($"Directory {directory} does not exist, creating");
                Directory.CreateDirectory(directory);
            }
            if (!DoFileExist(sourcePath))
            {
                Logger.Error($"File {sourcePath} does not exist");
                throw new FileNotFoundException($"File {sourcePath} does not exist");
            }
            if (DoFileExist(destinationPath)) Logger.Warn($"File {destinationPath} already exists, overwriting");

            File.Copy(sourcePath, destinationPath, true);
        }

        public void DeleteFile(string path)
        {
            string directory = Path.GetDirectoryName(path) ?? throw new ArgumentNullException($"Impossible to get directory from {path}");
            if (!Directory.Exists(directory))
            {
                Logger.Error($"Directory {directory} does not exist");
                throw new DirectoryNotFoundException($"Directory {directory} does not exist");
            }
            if (!DoFileExist(path)) Logger.Warn($"File {path} does not exist");

            File.Delete(path);
        }

        public void Dispose()
        {
            // No resources to dispose in this class
        }
    }
}
