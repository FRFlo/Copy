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

        public bool DoFileExist(ListResult element)
        {
            return File.Exists(element.ToString());
        }

        [SupportedOSPlatform("windows")]
        public ListResult[] ListFiles(string path, CopyFilter filter)
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
                .Select(f => new ListResult(path, new FileInfo(f).Name))
                .ToArray();
        }

        public Stream GetFile(ListResult element)
        {
            string elementPath = element.ToString();
            string directory = Path.GetDirectoryName(elementPath) ?? throw new ArgumentNullException($"Impossible to get directory from {elementPath}");
            if (!Directory.Exists(directory))
            {
                Logger.Error($"Directory {directory} does not exist");
                throw new DirectoryNotFoundException($"Directory {directory} does not exist");
            }
            if (!DoFileExist(element))
            {
                Logger.Error($"File {elementPath} does not exist");
                throw new FileNotFoundException($"File {elementPath} does not exist");
            }

            return File.OpenRead(elementPath);
        }

        public void PutFile(ListResult element, Stream stream)
        {
            string elementPath = element.ToString();
            string directory = Path.GetDirectoryName(elementPath) ?? throw new ArgumentNullException($"Impossible to get directory from {elementPath}");
            if (!Directory.Exists(directory))
            {
                Logger.Warn($"Directory {directory} does not exist, creating");
                Directory.CreateDirectory(directory);
            }
            if (DoFileExist(element)) Logger.Warn($"File {elementPath} already exists, overwriting");

            using var fileStream = File.Create(elementPath);
            stream.CopyTo(fileStream);
        }

        public void MoveElement(ListResult sourceElement, ListResult destinationElement)
        {
            string sourceElementPath = sourceElement.ToString();
            string destinationElementPath = destinationElement.ToString();
            string directory = Path.GetDirectoryName(destinationElementPath) ?? throw new ArgumentNullException($"Impossible to get directory from {destinationElementPath}");
            if (!Directory.Exists(directory))
            {
                Logger.Warn($"Directory {directory} does not exist, creating");
                Directory.CreateDirectory(directory);
            }
            if (!DoFileExist(sourceElement))
            {
                Logger.Error($"File {sourceElementPath} does not exist");
                throw new FileNotFoundException($"File {sourceElementPath} does not exist");
            }
            if (DoFileExist(destinationElement)) Logger.Warn($"File {destinationElementPath} already exists, overwriting");

            File.Move(sourceElementPath, destinationElementPath);
        }

        public void CopyElement(ListResult sourceElement, ListResult destinationElement)
        {
            string sourceElementPath = sourceElement.ToString();
            string destinationElementPath = destinationElement.ToString();
            string directory = Path.GetDirectoryName(destinationElementPath) ?? throw new ArgumentNullException($"Impossible to get directory from {destinationElementPath}");
            if (!Directory.Exists(directory))
            {
                Logger.Warn($"Directory {directory} does not exist, creating");
                Directory.CreateDirectory(directory);
            }
            if (!DoFileExist(sourceElement))
            {
                Logger.Error($"File {sourceElementPath} does not exist");
                throw new FileNotFoundException($"File {sourceElementPath} does not exist");
            }
            if (DoFileExist(destinationElement)) Logger.Warn($"File {destinationElementPath} already exists, overwriting");

            File.Copy(sourceElementPath, destinationElementPath, true);
        }

        public void DeleteElement(ListResult element)
        {
            string elementPath = element.ToString();
            string directory = Path.GetDirectoryName(elementPath) ?? throw new ArgumentNullException($"Impossible to get directory from {elementPath}");
            if (!Directory.Exists(directory))
            {
                Logger.Error($"Directory {directory} does not exist");
                throw new DirectoryNotFoundException($"Directory {directory} does not exist");
            }
            if (!DoFileExist(element)) Logger.Warn($"File {elementPath} does not exist");

            File.Delete(elementPath);
        }

        public void Dispose()
        {
            // No resources to dispose in this class
        }
    }
}
