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

        public bool DoFileExist(ListResult element)
        {
            return FtpClient.FileExists(element.ToString());
        }

        public ListResult[] ListFiles(string path, CopyFilter filter)
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
            return files.Select(f => new ListResult(path, f.Name)).ToArray();
        }

        public Stream GetFile(ListResult element)
        {
            string elementPath = element.ToString();
            string directory = Path.GetDirectoryName(elementPath) ?? throw new ArgumentNullException($"Impossible to get directory from {elementPath}");
            if (!FtpClient.DirectoryExists(directory))
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
            FtpClient.DownloadStream(stream, elementPath);
            stream.Position = 0;
            return stream;
        }

        public void PutFile(ListResult element, Stream stream)
        {
            string elementPath = element.ToString();
            string directory = Path.GetDirectoryName(elementPath) ?? throw new ArgumentNullException($"Impossible to get directory from {elementPath}");
            if (!FtpClient.DirectoryExists(directory))
            {
                Logger.Warn($"Directory {directory} does not exist, creating");
                FtpClient.CreateDirectory(directory);
            }
            if (DoFileExist(element)) Logger.Warn($"File {elementPath} already exists, overwriting");

            FtpClient.UploadStream(stream, elementPath, FtpRemoteExists.Overwrite);
        }

        public void MoveElement(ListResult sourceElement, ListResult destinationElement)
        {
            string sourceElementPath = sourceElement.ToString();
            string destinationElementPath = destinationElement.ToString();
            string directory = Path.GetDirectoryName(destinationElementPath) ?? throw new ArgumentNullException($"Impossible to get directory from {destinationElementPath}");
            if (!FtpClient.DirectoryExists(directory))
            {
                Logger.Warn($"Directory {directory} does not exist, creating");
                FtpClient.CreateDirectory(directory);
            }
            if (!DoFileExist(sourceElement))
            {
                Logger.Error($"File {sourceElementPath} does not exist");
                throw new FileNotFoundException($"File {sourceElementPath} does not exist");
            }
            if (DoFileExist(destinationElement)) Logger.Warn($"File {destinationElementPath} already exists, overwriting");

            FtpClient.MoveFile(sourceElementPath, destinationElementPath, FtpRemoteExists.Overwrite);
        }

        public void CopyElement(ListResult sourceElement, ListResult destinationElement)
        {
            string sourceElementPath = sourceElement.ToString();
            string destinationElementPath = destinationElement.ToString();
            string directory = Path.GetDirectoryName(destinationElementPath) ?? throw new ArgumentNullException($"Impossible to get directory from {destinationElementPath}");
            if (!FtpClient.DirectoryExists(directory))
            {
                Logger.Warn($"Directory {directory} does not exist, creating");
                FtpClient.CreateDirectory(directory);
            }
            if (!DoFileExist(sourceElement))
            {
                Logger.Error($"File {sourceElementPath} does not exist");
                throw new FileNotFoundException($"File {sourceElementPath} does not exist");
            }
            if (DoFileExist(destinationElement)) Logger.Warn($"File {destinationElementPath} already exists, overwriting");

            FtpClient.TransferFile(sourceElementPath, FtpClient, destinationElementPath, existsMode: FtpRemoteExists.Overwrite);
        }

        public void DeleteElement(ListResult element)
        {
            string elementPath = element.ToString();
            string directory = Path.GetDirectoryName(elementPath) ?? throw new ArgumentNullException($"Impossible to get directory from {elementPath}");
            if (!FtpClient.DirectoryExists(directory))
            {
                Logger.Error($"Directory {directory} does not exist");
                throw new DirectoryNotFoundException($"Directory {directory} does not exist");
            }
            if (!DoFileExist(element)) Logger.Warn($"File {elementPath} does not exist");

            FtpClient.DeleteFile(elementPath);
        }

        public void Dispose()
        {
            FtpClient.Dispose();
        }
    }
}
