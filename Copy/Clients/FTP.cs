using Copy.Types;
using FluentFTP;
using System.Net;
using System.Text.RegularExpressions;

namespace Copy.Clients
{
    /// <summary>
    /// FTP client.
    /// </summary>
    internal class FTP : IClient
    {
        /// <summary>
        /// Client credentials.
        /// </summary>
        private readonly Client _credentials;
        /// <summary>
        /// FTP client.
        /// </summary>
        private readonly FtpClient FtpClient;

        public Client Config => _credentials;

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="credentials"></param>
        public FTP(Client credentials)
        {
            _credentials = credentials;
            FtpClient = new FtpClient(credentials.Host, new NetworkCredential(credentials.Username, credentials.Password), credentials.Port);
            FtpClient.Config.EncryptionMode = FtpEncryptionMode.Auto;
            FtpClient.ValidateCertificate += (client, args) =>
            {
                if (credentials.Fingerprint == null)
                {
                    args.Accept = true;
                }
                else if (args.Certificate.GetCertHashString() == credentials.Fingerprint)
                {
                    args.Accept = true;
                }
                else
                {
                    args.Accept = false;
                }
            };
            FtpClient.AutoConnect();
        }

        public bool DoFileExist(string path)
        {
            return FtpClient.FileExists(path);
        }

        public string[] ListFiles(string path, CopyFilter filter)
        {
            if (!FtpClient.DirectoryExists(path))
            {
                Logger.Error($"Directory {path} does not exist");
                throw new DirectoryNotFoundException($"Directory {path} does not exist");
            }

            Regex nameRegex = new(filter.Name);
            Regex authorRegex = new(filter.Author);

            FtpListItem[] files = FtpClient.GetListing(path)
                .Where(f => nameRegex.IsMatch(f.Name) && authorRegex.IsMatch(f.RawOwner) && f.Created >= filter.CreatedAfter && (ulong)f.Size <= filter.MaxSize && (ulong)f.Size >= filter.MinSize)
                .ToArray();
            return files.Select(f => Path.Combine(path, f.Name)).ToArray();
        }

        public Stream GetFile(string path)
        {
            string directory = Path.GetDirectoryName(path) ?? throw new ArgumentNullException($"Impossible to get directory from {path}");
            if (!FtpClient.DirectoryExists(directory))
            {
                Logger.Error($"Directory {directory} does not exist");
                throw new DirectoryNotFoundException($"Directory {directory} does not exist");
            }
            if (!DoFileExist(path))
            {
                Logger.Error($"File {path} does not exist");
                throw new FileNotFoundException($"File {path} does not exist");
            }

            var stream = new MemoryStream();
            FtpClient.DownloadStream(stream, path);
            stream.Position = 0;
            return stream;
        }

        public void PutFile(string path, Stream stream)
        {
            string directory = Path.GetDirectoryName(path) ?? throw new ArgumentNullException($"Impossible to get directory from {path}");
            if (!FtpClient.DirectoryExists(directory))
            {
                Logger.Warn($"Directory {directory} does not exist, creating");
                FtpClient.CreateDirectory(directory);
            }
            if (DoFileExist(path)) Logger.Warn($"File {path} already exists, overwriting");

            FtpClient.UploadStream(stream, path, FtpRemoteExists.Overwrite);
        }

        public void MoveFile(string sourcePath, string destinationPath)
        {
            string directory = Path.GetDirectoryName(destinationPath) ?? throw new ArgumentNullException($"Impossible to get directory from {destinationPath}");
            if (!FtpClient.DirectoryExists(directory))
            {
                Logger.Warn($"Directory {directory} does not exist, creating");
                FtpClient.CreateDirectory(directory);
            }
            if (!DoFileExist(sourcePath))
            {
                Logger.Error($"File {sourcePath} does not exist");
                throw new FileNotFoundException($"File {sourcePath} does not exist");
            }
            if (DoFileExist(destinationPath)) Logger.Warn($"File {destinationPath} already exists, overwriting");

            FtpClient.MoveFile(sourcePath, destinationPath, FtpRemoteExists.Overwrite);
        }

        public void CopyFile(string sourcePath, string destinationPath)
        {
            string directory = Path.GetDirectoryName(destinationPath) ?? throw new ArgumentNullException($"Impossible to get directory from {destinationPath}");
            if (!FtpClient.DirectoryExists(directory))
            {
                Logger.Warn($"Directory {directory} does not exist, creating");
                FtpClient.CreateDirectory(directory);
            }
            if (!DoFileExist(sourcePath))
            {
                Logger.Error($"File {sourcePath} does not exist");
                throw new FileNotFoundException($"File {sourcePath} does not exist");
            }
            if (DoFileExist(destinationPath)) Logger.Warn($"File {destinationPath} already exists, overwriting");

            FtpClient.TransferFile(sourcePath, FtpClient, destinationPath, existsMode: FtpRemoteExists.Overwrite);
        }

        public void DeleteFile(string path)
        {
            string directory = Path.GetDirectoryName(path) ?? throw new ArgumentNullException($"Impossible to get directory from {path}");
            if (!FtpClient.DirectoryExists(directory))
            {
                Logger.Error($"Directory {directory} does not exist");
                throw new DirectoryNotFoundException($"Directory {directory} does not exist");
            }
            if (!DoFileExist(path)) Logger.Warn($"File {path} does not exist");

            FtpClient.DeleteFile(path);
        }

        public void Dispose()
        {
            FtpClient.Dispose();
        }
    }
}
