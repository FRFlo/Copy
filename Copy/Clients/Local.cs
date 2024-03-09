using Copy.Types;
using System;
using System.IO;

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

        public Client Credentials => _credentials;

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

        public string[] ListFiles(string path)
        {
            if (!Directory.Exists(path))
            {
                Logger.Error($"Directory {path} does not exist");
                throw new DirectoryNotFoundException($"Directory {path} does not exist");
            }

            return Directory.GetFiles(path);
        }

        public Stream GetFile(string path)
        {
            if (!DoFileExist(path))
            {
                Logger.Error($"File {path} does not exist");
                throw new FileNotFoundException($"File {path} does not exist");
            }

            return File.OpenRead(path);
        }

        public void PutFile(string path, Stream stream)
        {
            if (DoFileExist(path)) Logger.Warn($"File {path} already exists, overwriting");

            using var fileStream = File.Create(path);
            stream.CopyTo(fileStream);
        }

        public void DeleteFile(string path)
        {
            if (!DoFileExist(path)) Logger.Warn($"File {path} does not exist");

            File.Delete(path);
        }

        public void Dispose()
        {
            // No resources to dispose in this class
        }
    }
}
