using Copy.Types;
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

        public Client Config => _credentials;

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="credentials"></param>
        public Exchange(Client credentials)
        {
            _credentials = credentials;

            if (credentials.Autodiscover)
            {
                ExchangeService = new ExchangeService()
                {
                    Credentials = new WebCredentials(credentials.Username, credentials.Password)
                };
                ExchangeService.AutodiscoverUrl(credentials.Username);
            }
            else
            {
                ExchangeService = new ExchangeService()
                {
                    Credentials = new WebCredentials(credentials.Username, credentials.Password),
                    Url = new Uri(credentials.Host)
                };
            }
        }

        public bool DoFileExist(ListResult element)
        {
            throw new NotImplementedException();
        }

        public ListResult[] ListFiles(string path, CopyFilter filter)
        {
            throw new NotImplementedException();
        }

        public Stream GetFile(ListResult element)
        {
            throw new NotImplementedException();
        }

        public void PutFile(ListResult element, Stream stream)
        {
            throw new NotImplementedException();
        }

        public void MoveElement(ListResult sourceElement, ListResult destinationElement)
        {
            throw new NotImplementedException();
        }

        public void CopyElement(ListResult sourceElement, ListResult destinationElement)
        {
            throw new NotImplementedException();
        }

        public void DeleteElement(ListResult element)
        {
            throw new NotImplementedException();
        }

        public void Dispose()
        {
            throw new NotImplementedException();
        }
    }
}
