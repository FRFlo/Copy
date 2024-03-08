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
        [JsonProperty("Debug", Required = Required.DisallowNull)]
        public bool Debug { get; set; } = false;
        /// <summary>
        /// List of clients.
        /// </summary>
        [JsonProperty("Clients", Required = Required.AllowNull)]
        public List<Client> Clients = [];

        /// <summary>
        /// List of tasks.
        /// </summary>
        [JsonProperty("Tasks", Required = Required.AllowNull)]
        public List<CopyTask> Tasks = [];

        /// <summary>
        /// Load the configuration from the file.
        /// </summary>
        /// <param name="path">Path to the configuration file.</param>
        /// <returns>Configuration.</returns>
        public static Config Load(string path)
        {
            using FileStream stream = new(path, FileMode.Open);
            using StreamReader reader = new(stream);
            string json = reader.ReadToEnd();
            return JsonConvert.DeserializeObject<Config>(json) ?? throw new InvalidDataException("Cannot deserialize the configuration.");
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
                        Client = "FTP",
                        Source = "source",
                        Destination = "destination",
                        Delete = true
                    },
                    new CopyTask()
                    {
                        Source = "source",
                        Destination = "destination",
                        Client = "SFTP",
                        Filter = "*.txt"
                    },
                    new CopyTask()
                    {
                        Source = "source",
                        Destination = "destination",
                        Client = "Local",
                        Delete = true,
                        Filter = "*.txt"
                    },
                    new CopyTask()
                    {
                        Source = "source",
                        Destination = "destination",
                        Client = "Exchange"
                    }
                ]
            }, Formatting.Indented);
        }
    }
}
