﻿using Copy.Types;
using FluentFTP;
using System.Net;

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
        /// <summary>
        /// Client credentials.
        /// </summary>
        public Client Credentials => _credentials;

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="credentials"></param>
        public FTP(Client credentials)
        {
            _credentials = credentials;
            FtpClient = new FtpClient(credentials.Host, new NetworkCredential(credentials.Username, credentials.Password), credentials.Port);
            FtpClient.AutoConnect();
        }

        public bool DoFileExist(string path)
        {
            return FtpClient.FileExists(path);
        }

        public string[] ListFiles(string path)
        {
            if (!FtpClient.DirectoryExists(path))
            {
                Logger.Error($"Directory {path} does not exist");
                throw new DirectoryNotFoundException($"Directory {path} does not exist");
            }

            FtpListItem[] files = FtpClient.GetListing(path);
            return files.Select(f => f.Name).ToArray();
        }

        public Stream GetFile(string path)
        {
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
            if (DoFileExist(path)) Logger.Warn($"File {path} already exists, overwriting");

            FtpClient.UploadStream(stream, path, FtpRemoteExists.Overwrite);
        }

        public void DeleteFile(string path)
        {
            if (!DoFileExist(path)) Logger.Warn($"File {path} does not exist");

            FtpClient.DeleteFile(path);
        }

        public void Dispose()
        {
            FtpClient.Dispose();
        }
    }
}
