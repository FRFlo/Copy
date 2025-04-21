using Copy.Types;
using Newtonsoft.Json;
using Newtonsoft.Json.Schema.Generation;

namespace Copy
{
    /// <summary>
    /// Json serializable configuration class.
    /// </summary>
    internal class Config
    {
        /// <summary>
        /// Path to the configuration file.
        /// </summary>
        public static string ConfigPath = Path.Combine(Directory.GetCurrentDirectory(), "config.json");
        /// <summary>
        /// Path to the scheme file.
        /// </summary>
        public static string SchemePath = Path.Combine(Directory.GetCurrentDirectory(), "scheme.json");

        /// <summary>
        /// Indicates if the program is in debug mode.
        /// </summary>
        [JsonProperty(nameof(Debug), Required = Required.DisallowNull)]
        public bool Debug { get; set; } = false;
        /// <summary>
        /// List of clients.
        /// </summary>
        [JsonProperty(nameof(Clients), Required = Required.AllowNull)]
        public List<Client> Clients = [];

        /// <summary>
        /// List of tasks.
        /// </summary>
        [JsonProperty(nameof(Tasks), Required = Required.AllowNull)]
        public List<CopyTask> Tasks { get; set; } = [];

        /// <summary>
        /// Load the configuration from the file and environment variables
        /// </summary>
        /// <param name="path">Path to the configuration file</param>
        /// <returns>Configuration</returns>
        public static Config Load(string path)
        {
            using FileStream stream = new(path, FileMode.Open);
            using StreamReader reader = new(stream);
            string json = reader.ReadToEnd();
            var config = JsonConvert.DeserializeObject<Config>(json) ?? throw new InvalidDataException("Cannot deserialize the configuration.");
            
            // Override with environment variables
            foreach (var client in config.Clients)
            {
                string prefix = $"COPY_CLIENT_{client.Name.ToUpper()}_";
                string? host = Environment.GetEnvironmentVariable(prefix + "HOST");
                if (!string.IsNullOrEmpty(host))
                    client.Host = host;
                
                string? username = Environment.GetEnvironmentVariable(prefix + "USERNAME");
                if (!string.IsNullOrEmpty(username))
                    client.Username = username;
                
                string? password = Environment.GetEnvironmentVariable(prefix + "PASSWORD");
                if (!string.IsNullOrEmpty(password))
                    client.Password = password;
                
                string? privateKey = Environment.GetEnvironmentVariable(prefix + "PRIVATEKEY");
                if (!string.IsNullOrEmpty(privateKey))
                    client.PrivateKey = privateKey;
                
                if (int.TryParse(Environment.GetEnvironmentVariable(prefix + "PORT"), out int port))
                    client.Port = port;
            }

            // Validate the configuration
            config.Validate();
            
            return config;
        }

        /// <summary>
        /// Obtain the JSON scheme of the configuration.
        /// </summary>
        /// <returns>JSON scheme of the configuration.</returns>
        public static string GetScheme()
        {
            return new JSchemaGenerator().Generate(typeof(Config)).ToString();
        }
        /// <summary>
        /// Obtain the default configuration.
        /// </summary>
        /// <returns>Default configuration.</returns>
        public static string GetDefault()
        {
            return JsonConvert.SerializeObject(new Config()
            {

                Clients = [
                    new Client()
                    {
                        Type = ClientType.FTP,
                        Name = "FTP",
                        Host = "ftp.example.com",
                        Port = 21,
                        Username = "user",
                        Password = "password"
                    },
                    new Client()
                    {
                        Type = ClientType.SFTP,
                        Name = "SFTP",
                        Host = "sftp.example.com",
                        Port = 22,
                        Username = "user",
                        Password = "password"
                    },
                    new Client()
                    {
                        Type = ClientType.Local,
                        Name = "Local",
                        Host = "localhost",
                    },
                    new Client()
                    {
                        Type = ClientType.Exchange,
                        Name = "Exchange",
                        Host = "exchange.example.com",
                        Username = "user@example.com",
                        Password = "password"
                    }
                ],
                Tasks = [
                    new CopyTask()
                    {
                        Source = new CopyIO("FTP", "source"),
                        Destination =  new CopyIO("FTP", "destination"),
                        Delete = true
                    },
                    new CopyTask()
                    {
                        Source =  new CopyIO("SFTP", "source"),
                        Destination =  new CopyIO("SFTP", "destination"),
                        Filter = new CopyFilter()
                        {
                            Name = ".*\\.txt",
                            Author = "root"
                        }
                    },
                    new CopyTask()
                    {
                        Source =  new CopyIO("Local", "source"),
                        Destination = new CopyIO("Local", "destination"),
                        Delete = true,
                        Filter = new CopyFilter()
                        {
                            CreatedAfter = DateTime.Now.AddDays(-1),
                            MinSize = 512,
                            MaxSize = 4096
                        }
                    },
                    new CopyTask()
                    {
                        Source = new CopyIO("Exchange", "source"),
                        Destination = new CopyIO("Exchange", "destination"),
                    }
                ]
            }, Formatting.Indented);
        }

        /// <summary>
        /// Validate the configuration
        /// </summary>
        /// <exception cref="InvalidDataException">Thrown when configuration is invalid</exception>
        public void Validate()
        {
            if (Clients == null || Clients.Count == 0)
            {
                throw new InvalidDataException("La configuration doit contenir au moins un client");
            }

            foreach (var client in Clients)
            {
                if (string.IsNullOrEmpty(client.Name))
                {
                    throw new InvalidDataException("Le nom du client ne peut pas être vide");
                }
                if (string.IsNullOrEmpty(client.Host))
                {
                    throw new InvalidDataException($"L'hôte du client {client.Name} ne peut pas être vide");
                }
                
                // Validation spécifique selon le type de client
                switch (client.Type)
                {
                    case ClientType.FTP:
                    case ClientType.Exchange:
                        if (string.IsNullOrEmpty(client.Username))
                        {
                            throw new InvalidDataException($"Le nom d'utilisateur est requis pour le client {client.Name} de type {client.Type}");
                        }
                        if (string.IsNullOrEmpty(client.Password))
                        {
                            throw new InvalidDataException($"Le mot de passe est requis pour le client {client.Name}");
                        }
                        break;
                    case ClientType.SFTP:
                        if (string.IsNullOrEmpty(client.Username))
                        {
                            throw new InvalidDataException($"Le nom d'utilisateur est requis pour le client {client.Name} de type {client.Type}");
                        }
                        if (string.IsNullOrEmpty(client.Password) && string.IsNullOrEmpty(client.PrivateKey))
                        {
                            throw new InvalidDataException($"Un mot de passe ou une clé privée est requis pour le client {client.Name}");
                        }
                        break;
                }
            }

            if (Tasks == null || Tasks.Count == 0)
            {
                throw new InvalidDataException("La configuration doit contenir au moins une tâche");
            }

            var clientNames = Clients.Select(c => c.Name).ToHashSet();
            foreach (var task in Tasks)
            {
                if (string.IsNullOrEmpty(task.Source.Client))
                {
                    throw new InvalidDataException("La source de la tâche ne peut pas être vide");
                }
                if (string.IsNullOrEmpty(task.Destination.Client))
                {
                    throw new InvalidDataException("La destination de la tâche ne peut pas être vide");
                }
                if (!clientNames.Contains(task.Source.Client))
                {
                    throw new InvalidDataException($"Le client source {task.Source.Client} n'existe pas");
                }
                if (!clientNames.Contains(task.Destination.Client))
                {
                    throw new InvalidDataException($"Le client destination {task.Destination.Client} n'existe pas");
                }
            }
        }
    }
}
