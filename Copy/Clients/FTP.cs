using Copy.Types;
using FluentFTP;
using System.Net;
using System.Text.RegularExpressions;

namespace Copy.Clients
{
    /// <summary>
    ///     FTP client.
    /// </summary>
    internal class FTP : IClient
    {
        /// <summary>
        ///     FTP client.
        /// </summary>
        private readonly FtpClient FtpClient;

        /// <summary>
        ///     Constructor.
        /// </summary>
        /// <param name="credentials"></param>
        public FTP(Client credentials)
        {
            Config = credentials;
            using IDisposable scope = Logger.BeginScope(
                ("phase", "auth"),
                ("protocol", nameof(FTP)),
                ("client", credentials.Name),
                ("host", credentials.Host),
                ("port", credentials.Port.ToString()));

            Logger.Info("Starting FTP authentication");

            try
            {
                FtpClient = new FtpClient(credentials.Host,
                    new NetworkCredential(credentials.Username, credentials.Password), credentials.Port);
                FtpClient.Config.EncryptionMode = FtpEncryptionMode.Auto;
                FtpClient.ValidateCertificate += (client, args) =>
                {
                    if (credentials.Fingerprint == null)
                    {
                        args.Accept = true;
                        Logger.Debug("Accepted FTP certificate because no fingerprint is configured");
                    }
                    else if (args.Certificate.GetCertHashString() == credentials.Fingerprint)
                    {
                        args.Accept = true;
                        Logger.Debug("Accepted FTP certificate because fingerprint matched");
                    }
                    else
                    {
                        args.Accept = false;
                        Logger.Warn("Rejected FTP certificate because fingerprint did not match");
                    }
                };
                FtpClient.AutoConnect();
                Logger.Info("FTP authentication succeeded");
            }
            catch (Exception ex)
            {
                Logger.Error("FTP authentication failed", ex);
                throw;
            }
        }

        /// <summary>
        ///     Client credentials.
        /// </summary>
        public Client Config { get; }

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
                .Where(f => nameRegex.IsMatch(f.Name) && authorRegex.IsMatch(f.RawOwner) &&
                            f.Created >= filter.CreatedAfter && (ulong)f.Size <= filter.MaxSize &&
                            (ulong)f.Size >= filter.MinSize)
                .ToArray();
            return files.Select(f => Path.Combine(path, f.Name)).ToArray();
        }

        public Stream GetFile(string path)
        {
            string directory = Path.GetDirectoryName(path) ??
                               throw new ArgumentNullException($"Impossible to get directory from {path}");
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

            MemoryStream stream = new();
            FtpClient.DownloadStream(stream, path);
            stream.Position = 0;
            return stream;
        }

        public void PutFile(string path, Stream stream)
        {
            string directory = Path.GetDirectoryName(path) ??
                               throw new ArgumentNullException($"Impossible to get directory from {path}");
            if (!FtpClient.DirectoryExists(directory))
            {
                Logger.Warn($"Directory {directory} does not exist, creating");
                FtpClient.CreateDirectory(directory);
            }

            if (DoFileExist(path))
            {
                Logger.Warn($"File {path} already exists, overwriting");
            }

            FtpClient.UploadStream(stream, path);
        }

        public void MoveFile(string sourcePath, string destinationPath)
        {
            string directory = Path.GetDirectoryName(destinationPath) ??
                               throw new ArgumentNullException($"Impossible to get directory from {destinationPath}");
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

            if (DoFileExist(destinationPath))
            {
                Logger.Warn($"File {destinationPath} already exists, overwriting");
            }

            FtpClient.MoveFile(sourcePath, destinationPath);
        }

        public void CopyFile(string sourcePath, string destinationPath)
        {
            string directory = Path.GetDirectoryName(destinationPath) ??
                               throw new ArgumentNullException($"Impossible to get directory from {destinationPath}");
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

            if (DoFileExist(destinationPath))
            {
                Logger.Warn($"File {destinationPath} already exists, overwriting");
            }

            FtpClient.TransferFile(sourcePath, FtpClient, destinationPath, existsMode: FtpRemoteExists.Overwrite);
        }

        public void DeleteFile(string path)
        {
            string directory = Path.GetDirectoryName(path) ??
                               throw new ArgumentNullException($"Impossible to get directory from {path}");
            if (!FtpClient.DirectoryExists(directory))
            {
                Logger.Error($"Directory {directory} does not exist");
                throw new DirectoryNotFoundException($"Directory {directory} does not exist");
            }

            if (!DoFileExist(path))
            {
                Logger.Warn($"File {path} does not exist");
            }

            FtpClient.DeleteFile(path);
        }

        public void Dispose()
        {
            FtpClient.Dispose();
        }
    }
}