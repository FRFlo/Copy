using Copy.Types;
using Renci.SshNet;

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

        public Client Credentials => _credentials;

        public SFTP(Client credentials)
        {
            _credentials = credentials;
            SftpClient = new SftpClient(credentials.Host, credentials.Port, credentials.Username, credentials.Password);
            SftpClient.Connect();
        }

        public string[] ListFiles(string path)
        {
            if (!SftpClient.Exists(path))
            {
                Logger.Error($"Directory {path} does not exist");
                throw new DirectoryNotFoundException($"Directory {path} does not exist");
            }

            var files = SftpClient.ListDirectory(path);
            return files.Select(f => f.Name).ToArray();
        }

        public bool DoFileExist(string path)
        {
            return SftpClient.Exists(path);
        }

        public Stream GetFile(string path)
        {
            string directory = Path.GetDirectoryName(path) ?? throw new ArgumentNullException($"Impossible to get directory from {path}");
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

            var stream = new MemoryStream();
            SftpClient.DownloadFile(path, stream);
            stream.Position = 0;
            return stream;
        }

        public void PutFile(string path, Stream stream)
        {
            string directory = Path.GetDirectoryName(path) ?? throw new ArgumentNullException($"Impossible to get directory from {path}");
            if (!SftpClient.Exists(directory))
            {
                Logger.Warn($"Directory {directory} does not exist, creating");
                SftpClient.CreateDirectory(directory);
            }
            if (DoFileExist(path)) Logger.Warn($"File {path} already exists, overwriting");

            SftpClient.UploadFile(stream, path, true);
        }

        public void DeleteFile(string path)
        {
            string directory = Path.GetDirectoryName(path) ?? throw new ArgumentNullException($"Impossible to get directory from {path}");
            if (!SftpClient.Exists(directory))
            {
                Logger.Error($"Directory {directory} does not exist");
                throw new DirectoryNotFoundException($"Directory {directory} does not exist");
            }
            if (!DoFileExist(path)) Logger.Warn($"File {path} does not exist");

            SftpClient.DeleteFile(path);
        }

        public void Dispose()
        {
            SftpClient.Disconnect();
            SftpClient.Dispose();
        }
    }
}
