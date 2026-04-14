using Copy.Types;
using Microsoft.Exchange.WebServices.Data;

namespace Copy.Clients
{
    internal class Exchange : IClient
    {
        /// <summary>
        ///     FTP client.
        /// </summary>
        private readonly ExchangeService ExchangeService;

        /// <summary>
        ///     Constructor.
        /// </summary>
        /// <param name="credentials"></param>
        public Exchange(Client credentials)
        {
            Config = credentials;
            using IDisposable scope = Logger.BeginScope(
                ("phase", "auth"),
                ("protocol", nameof(Exchange)),
                ("client", credentials.Name),
                ("host", credentials.Host),
                ("port", credentials.Port.ToString()),
                ("autodiscover", credentials.Autodiscover.ToString()));

            Logger.Info("Starting Exchange authentication");

            try
            {
                if (credentials.Autodiscover)
                {
                    ExchangeService = new ExchangeService
                    {
                        Credentials = new WebCredentials(credentials.Username, credentials.Password)
                    };
                    ExchangeService.AutodiscoverUrl(credentials.Username);
                    Logger.Info("Exchange autodiscover succeeded");
                }
                else
                {
                    ExchangeService = new ExchangeService
                    {
                        Credentials = new WebCredentials(credentials.Username, credentials.Password),
                        Url = new Uri(credentials.Host)
                    };
                    Logger.Info("Exchange service initialized with explicit URL");
                }

                Logger.Info("Exchange authentication configuration succeeded");
            }
            catch (Exception ex)
            {
                Logger.Error("Exchange authentication failed", ex);
                throw;
            }
        }

        /// <summary>
        ///     Client credentials.
        /// </summary>
        public Client Config { get; }

        public bool DoFileExist(string path)
        {
            throw new NotImplementedException();
        }

        public string[] ListFiles(string path, CopyFilter filter)
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

        public void MoveFile(string sourcePath, string destinationPath)
        {
            throw new NotImplementedException();
        }

        public void CopyFile(string sourcePath, string destinationPath)
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