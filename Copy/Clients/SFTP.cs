using Copy.Types;
using Renci.SshNet;
using Renci.SshNet.Sftp;
using System.Text.RegularExpressions;

namespace Copy.Clients
{
    internal class SFTP : IClient
    {
        /// <summary>
        ///     FTP client.
        /// </summary>
        private readonly SftpClient SftpClient;

        public SFTP(Client credentials)
        {
            Config = credentials;
            using IDisposable scope = Logger.BeginScope(
                ("phase", "auth"),
                ("protocol", nameof(SFTP)),
                ("host", credentials.Host),
                ("port", credentials.Port.ToString()),
                ("username", credentials.Username),
                ("authMode", string.IsNullOrEmpty(credentials.PrivateKey) ? "password" : "private-key"));

            Logger.Info("Starting SFTP authentication");

            try
            {
                if (credentials.Fingerprint == null)
                {
                    SftpClient = new SftpClient(credentials.Host, credentials.Port, credentials.Username,
                        credentials.Password);
                }
                else if (!string.IsNullOrEmpty(credentials.PrivateKey))
                {
                    using PrivateKeyFile privateKey = new(credentials.PrivateKey);
                    SftpClient = new SftpClient(credentials.Host, credentials.Port, credentials.Username, privateKey);
                }
                else
                {
                    SftpClient = new SftpClient(credentials.Host, credentials.Port, credentials.Username,
                        credentials.Password);
                }

                SftpClient.HostKeyReceived += (client, args) =>
                {
                    if (credentials.Fingerprint == null)
                    {
                        args.CanTrust = true;
                        Logger.Debug("Accepted SFTP host key because no fingerprint is configured");
                    }
                    else if (args.FingerPrintMD5 == credentials.Fingerprint)
                    {
                        args.CanTrust = true;
                        Logger.Debug("Accepted SFTP host key because fingerprint matched");
                    }
                    else
                    {
                        args.CanTrust = false;
                        Logger.Warn("Rejected SFTP host key because fingerprint did not match");
                    }
                };
                SftpClient.Connect();
                Logger.Info("SFTP authentication succeeded");
            }
            catch (Exception ex)
            {
                Logger.Error("SFTP authentication failed", ex);
                throw;
            }
        }

        /// <summary>
        ///     Client credentials.
        /// </summary>
        public Client Config { get; }

        public string[] ListFiles(string path, CopyFilter filter)
        {
            if (!SftpClient.Exists(path))
            {
                Logger.Error($"Directory {path} does not exist");
                throw new DirectoryNotFoundException($"Directory {path} does not exist");
            }

            if (filter.Author != ".*")
            {
                Logger.Warn("Filtering by author is not supported by SFTP");
            }

            Regex nameRegex = new(filter.Name);
            Regex authorRegex = new(filter.Author);

            IEnumerable<ISftpFile> files = SftpClient.ListDirectory(path)
                .Where(f => nameRegex.IsMatch(f.Name) && f.LastWriteTime >= filter.CreatedAfter &&
                            (ulong)f.Length <= filter.MaxSize && (ulong)f.Length >= filter.MinSize);
            return files.Select(f => f.FullName.Remove(0, 1)).ToArray();
        }

        public bool DoFileExist(string path)
        {
            return SftpClient.Exists(path);
        }

        public Stream GetFile(string path)
        {
            string directory = Path.GetDirectoryName(path) ??
                               throw new ArgumentNullException($"Impossible to get directory from {path}");
            if (!SftpClient.Exists(directory))
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
            SftpClient.DownloadFile(path, stream);
            stream.Position = 0;
            return stream;
        }

        public void PutFile(string path, Stream stream)
        {
            string directory = Path.GetDirectoryName(path) ??
                               throw new ArgumentNullException($"Impossible to get directory from {path}");
            if (!SftpClient.Exists(directory))
            {
                Logger.Warn($"Directory {directory} does not exist, creating");
                SftpClient.CreateDirectory(directory);
            }

            if (DoFileExist(path))
            {
                Logger.Warn($"File {path} already exists, overwriting");
            }

            SftpClient.UploadFile(stream, path, true);
        }

        public void MoveFile(string sourcePath, string destinationPath)
        {
            string directory = Path.GetDirectoryName(destinationPath) ??
                               throw new ArgumentNullException($"Impossible to get directory from {destinationPath}");
            if (!SftpClient.Exists(directory))
            {
                Logger.Warn($"Directory {directory} does not exist, creating");
                SftpClient.CreateDirectory(directory);
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

            SftpClient.RenameFile(sourcePath, destinationPath);
        }

        public void CopyFile(string sourcePath, string destinationPath)
        {
            string directory = Path.GetDirectoryName(destinationPath) ??
                               throw new ArgumentNullException($"Impossible to get directory from {destinationPath}");
            if (!SftpClient.Exists(directory))
            {
                Logger.Warn($"Directory {directory} does not exist, creating");
                SftpClient.CreateDirectory(directory);
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

            SftpClient.SynchronizeDirectories(Path.GetDirectoryName(sourcePath), directory,
                Path.GetFileName(sourcePath));
        }

        public void DeleteFile(string path)
        {
            string directory = Path.GetDirectoryName(path) ??
                               throw new ArgumentNullException($"Impossible to get directory from {path}");
            if (!SftpClient.Exists(directory))
            {
                Logger.Error($"Directory {directory} does not exist");
                throw new DirectoryNotFoundException($"Directory {directory} does not exist");
            }

            if (!DoFileExist(path))
            {
                Logger.Warn($"File {path} does not exist");
            }

            SftpClient.DeleteFile(path);
        }

        public void Dispose()
        {
            SftpClient.Disconnect();
            SftpClient.Dispose();
        }
    }
}