using Copy.Types;
using Renci.SshNet;
using System.Text.RegularExpressions;

namespace Copy.Clients
{
    internal class SFTP : IClient
    {
        /// <summary>
        /// Client credentials.
        /// </summary>
        private readonly Client _credentials;
        /// <summary>
        /// FTP client.
        /// </summary>
        private readonly SftpClient SftpClient;

        public Client Config => _credentials;

        public SFTP(Client credentials)
        {
            _credentials = credentials;
            if (credentials.Fingerprint != null)
            {
                using PrivateKeyFile privateKey = new(credentials.PrivateKey);
                SftpClient = new SftpClient(credentials.Host, credentials.Port, credentials.Username, privateKey);
            }
            else
            {
                SftpClient = new SftpClient(credentials.Host, credentials.Port, credentials.Username, credentials.Password);
            }
            SftpClient.HostKeyReceived += (client, args) =>
            {
                if (credentials.Fingerprint == null)
                {
                    args.CanTrust = true;
                }
                else if (args.FingerPrintMD5 == credentials.Fingerprint)
                {
                    args.CanTrust = true;
                }
                else
                {
                    args.CanTrust = false;
                }
            };
            SftpClient.Connect();
        }

        public ListResult[] ListFiles(string path, CopyFilter filter)
        {
            if (!SftpClient.Exists(path))
            {
                Logger.Error($"Directory {path} does not exist");
                throw new DirectoryNotFoundException($"Directory {path} does not exist");
            }

            if (filter.Author != ".*") Logger.Warn($"Filtering by author is not supported by SFTP");

            Regex nameRegex = new(filter.Name);
            Regex authorRegex = new(filter.Author);

            var files = SftpClient.ListDirectory(path)
                .Where(f => nameRegex.IsMatch(f.Name) && f.LastWriteTime >= filter.CreatedAfter && (ulong)f.Length <= filter.MaxSize && (ulong)f.Length >= filter.MinSize);
            return files.Select(f => new ListResult(path, f.Name)).ToArray();
        }

        public bool DoFileExist(ListResult element)
        {
            return SftpClient.Exists(element.ToString());
        }

        public Stream GetFile(ListResult element)
        {
            string elementPath = element.ToString();
            string directory = Path.GetDirectoryName(elementPath) ?? throw new ArgumentNullException($"Impossible to get directory from {elementPath}");
            if (!SftpClient.Exists(directory))
            {
                Logger.Error($"Directory {directory} does not exist");
                throw new DirectoryNotFoundException($"Directory {directory} does not exist");
            }
            if (!DoFileExist(element))
            {
                Logger.Error($"File {elementPath} does not exist");
                throw new FileNotFoundException($"File {elementPath} does not exist");
            }

            var stream = new MemoryStream();
            SftpClient.DownloadFile(elementPath, stream);
            stream.Position = 0;
            return stream;
        }

        public void PutFile(ListResult element, Stream stream)
        {
            string elementPath = element.ToString();
            string directory = Path.GetDirectoryName(elementPath) ?? throw new ArgumentNullException($"Impossible to get directory from {elementPath}");
            if (!SftpClient.Exists(directory))
            {
                Logger.Warn($"Directory {directory} does not exist, creating");
                SftpClient.CreateDirectory(directory);
            }
            if (DoFileExist(element)) Logger.Warn($"File {elementPath} already exists, overwriting");

            SftpClient.UploadFile(stream, elementPath, true);
        }

        public void MoveElement(ListResult sourceElement, ListResult destinationElement)
        {
            string sourceElementPath = sourceElement.ToString();
            string destinationElementPath = destinationElement.ToString();
            string directory = Path.GetDirectoryName(destinationElementPath) ?? throw new ArgumentNullException($"Impossible to get directory from {destinationElementPath}");
            if (!SftpClient.Exists(directory))
            {
                Logger.Warn($"Directory {directory} does not exist, creating");
                SftpClient.CreateDirectory(directory);
            }
            if (!DoFileExist(sourceElement))
            {
                Logger.Error($"File {sourceElementPath} does not exist");
                throw new FileNotFoundException($"File {sourceElementPath} does not exist");
            }
            if (DoFileExist(destinationElement)) Logger.Warn($"File {destinationElementPath} already exists, overwriting");

            SftpClient.RenameFile(sourceElementPath, destinationElementPath);
        }

        public void CopyElement(ListResult sourceElement, ListResult destinationElement)
        {
            string sourceElementPath = sourceElement.ToString();
            string destinationElementPath = destinationElement.ToString();
            string directory = Path.GetDirectoryName(destinationElementPath) ?? throw new ArgumentNullException($"Impossible to get directory from {destinationElementPath}");
            if (!SftpClient.Exists(directory))
            {
                Logger.Warn($"Directory {directory} does not exist, creating");
                SftpClient.CreateDirectory(directory);
            }
            if (!DoFileExist(sourceElement))
            {
                Logger.Error($"File {sourceElementPath} does not exist");
                throw new FileNotFoundException($"File {sourceElementPath} does not exist");
            }
            if (DoFileExist(destinationElement)) Logger.Warn($"File {destinationElementPath} already exists, overwriting");

            SftpClient.SynchronizeDirectories(Path.GetDirectoryName(sourceElementPath), directory, Path.GetFileName(sourceElementPath));
        }

        public void DeleteElement(ListResult element)
        {
            string elementPath = element.ToString();
            string directory = Path.GetDirectoryName(elementPath) ?? throw new ArgumentNullException($"Impossible to get directory from {elementPath}");
            if (!SftpClient.Exists(directory))
            {
                Logger.Error($"Directory {directory} does not exist");
                throw new DirectoryNotFoundException($"Directory {directory} does not exist");
            }
            if (!DoFileExist(element)) Logger.Warn($"File {elementPath} does not exist");

            SftpClient.DeleteFile(elementPath);
        }

        public void Dispose()
        {
            SftpClient.Disconnect();
            SftpClient.Dispose();
        }
    }
}
