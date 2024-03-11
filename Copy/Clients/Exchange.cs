﻿using Copy.Types;
using Microsoft.Exchange.WebServices.Data;

namespace Copy.Clients
{
    internal class Exchange : IClient
    {
        /// <summary>
        /// Client credentials.
        /// </summary>
        private readonly Client _credentials;
        /// <summary>
        /// FTP client.
        /// </summary>
        private readonly ExchangeService ExchangeService;

        public Client Credentials => _credentials;

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="credentials"></param>
        public Exchange(Client credentials)
        {
            _credentials = credentials;
            ExchangeService = new ExchangeService()
            {
                Credentials = new WebCredentials(credentials.Username, credentials.Password),
                Url = new Uri(credentials.Host)
            };
        }

        public bool DoFileExist(string path)
        {
            throw new NotImplementedException();
        }

        public string[] ListFiles(string path)
        {
            throw new NotImplementedException();
        }

        public Stream GetFile(string path)
        {
            throw new NotImplementedException();
        }

        public void PutFile(string path, Stream stream)
        {
            throw new NotImplementedException();
        }

        public void DeleteFile(string path)
        {
            throw new NotImplementedException();
        }

        public void Dispose()
        {
            throw new NotImplementedException();
        }
    }
}
