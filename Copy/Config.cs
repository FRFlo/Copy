using Copy.Types;
using Newtonsoft.Json;
using Newtonsoft.Json.Schema.Generation;

namespace Copy
{
    /// <summary>
    ///     Json serializable configuration class.
    /// </summary>
    internal class Config
    {
        /// <summary>
        ///     Path to the configuration file.
        /// </summary>
        public static string ConfigPath = Environment.GetEnvironmentVariable("CONFIG_PATH") ??
                                          Path.Combine(Directory.GetCurrentDirectory(), "config.json");

        /// <summary>
        ///     Path to the scheme file.
        /// </summary>
        public static string SchemePath = Environment.GetEnvironmentVariable("SCHEME_PATH") ??
                                          Path.Combine(Directory.GetCurrentDirectory(), "scheme.json");

        /// <summary>
        ///     Indicates if the program is in debug mode.
        /// </summary>
        [JsonProperty(nameof(Debug), Required = Required.DisallowNull)]
        public bool Debug { get; set; } = Environment.GetEnvironmentVariable("DEBUG")?.ToLower() == "true";

        /// <summary>
        ///     List of clients.
        /// </summary>
        [JsonProperty(nameof(Clients), Required = Required.AllowNull)]
        public List<Client> Clients { get; set; } = [];

        /// <summary>
        ///     List of tasks.
        /// </summary>
        [JsonProperty(nameof(Tasks), Required = Required.AllowNull)]
        public List<CopyTask> Tasks { get; set; } = [];

        /// <summary>
        ///     Mail recipients that receive warning and error notifications.
        /// </summary>
        [JsonProperty(nameof(MailTo), Required = Required.AllowNull)]
        [JsonConverter(typeof(SingleOrArrayConverter<string>))]
        public string[] MailTo { get; set; } = [];

        /// <summary>
        ///     SMTP settings used to send warning and error notifications.
        /// </summary>
        [JsonProperty(nameof(Smtp), Required = Required.AllowNull)]
        public SmtpSettings? Smtp { get; set; }

        /// <summary>
        ///     Load the configuration from the file and environment variables
        /// </summary>
        /// <param name="path">Path to the configuration file.</param>
        /// <returns>Configuration.</returns>
        public static Config FromFile(string path)
        {
            using FileStream stream = new(path, FileMode.Open);
            using StreamReader reader = new(stream);
            string json = reader.ReadToEnd();
            List<string> deserializationErrors = [];
            JsonSerializerSettings serializerSettings = new();
            serializerSettings.Error += (_, args) =>
            {
                deserializationErrors.Add(args.ErrorContext.Error.Message);
                args.ErrorContext.Handled = true;
            };

            Config? config;
            try
            {
                config = JsonConvert.DeserializeObject<Config>(json, serializerSettings);
            }
            catch (JsonException ex)
            {
                deserializationErrors.Add(ex.Message);
                throw new ConfigValidationException(deserializationErrors);
            }

            if (config == null)
            {
                deserializationErrors.Add("Cannot deserialize the configuration.");
                throw new ConfigValidationException(deserializationErrors);
            }

            config.Clients ??= [];
            config.Tasks ??= [];
            config.MailTo ??= [];

            bool hasSmtpEnvironmentOverride =
                !string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("COPY_SMTP_HOST")) ||
                !string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("COPY_SMTP_USERNAME")) ||
                !string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("COPY_SMTP_PASSWORD")) ||
                !string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("COPY_SMTP_FROM")) ||
                !string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("COPY_SMTP_PORT")) ||
                !string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("COPY_SMTP_ENABLESSL"));

            if (config.Smtp == null && hasSmtpEnvironmentOverride)
            {
                config.Smtp = new SmtpSettings();
            }

            // Override with environment variables
            foreach (Client client in config.Clients)
            {
                if (string.IsNullOrEmpty(client.Name))
                {
                    continue;
                }

                string prefix = $"COPY_CLIENT_{client.Name.ToUpper()}_";
                string? host = Environment.GetEnvironmentVariable(prefix + "HOST");
                if (!string.IsNullOrEmpty(host))
                {
                    client.Host = host;
                }

                string? username = Environment.GetEnvironmentVariable(prefix + "USERNAME");
                if (!string.IsNullOrEmpty(username))
                {
                    client.Username = username;
                }

                string? password = Environment.GetEnvironmentVariable(prefix + "PASSWORD");
                if (!string.IsNullOrEmpty(password))
                {
                    client.Password = password;
                }

                string? privateKey = Environment.GetEnvironmentVariable(prefix + "PRIVATEKEY");
                if (!string.IsNullOrEmpty(privateKey))
                {
                    client.PrivateKey = privateKey;
                }

                if (int.TryParse(Environment.GetEnvironmentVariable(prefix + "PORT"), out int port))
                {
                    client.Port = port;
                }
            }

            if (config.Smtp != null)
            {
                string? smtpHost = Environment.GetEnvironmentVariable("COPY_SMTP_HOST");
                if (!string.IsNullOrWhiteSpace(smtpHost))
                {
                    config.Smtp.Host = smtpHost;
                }

                string? smtpUsername = Environment.GetEnvironmentVariable("COPY_SMTP_USERNAME");
                if (!string.IsNullOrWhiteSpace(smtpUsername))
                {
                    config.Smtp.Username = smtpUsername;
                }

                string? smtpPassword = Environment.GetEnvironmentVariable("COPY_SMTP_PASSWORD");
                if (!string.IsNullOrWhiteSpace(smtpPassword))
                {
                    config.Smtp.Password = smtpPassword;
                }

                string? smtpFrom = Environment.GetEnvironmentVariable("COPY_SMTP_FROM");
                if (!string.IsNullOrWhiteSpace(smtpFrom))
                {
                    config.Smtp.From = smtpFrom;
                }

                if (int.TryParse(Environment.GetEnvironmentVariable("COPY_SMTP_PORT"), out int smtpPort))
                {
                    config.Smtp.Port = smtpPort;
                }

                if (bool.TryParse(Environment.GetEnvironmentVariable("COPY_SMTP_ENABLESSL"), out bool smtpEnableSsl))
                {
                    config.Smtp.EnableSsl = smtpEnableSsl;
                }
            }

            // Validate the configuration
            List<string> validationErrors = config.GetValidationErrors();
            if (deserializationErrors.Count > 0 || validationErrors.Count > 0)
            {
                throw new ConfigValidationException([.. deserializationErrors, .. validationErrors]);
            }

            return config;
        }

        /// <summary>
        ///     Obtain the JSON scheme of the configuration.
        /// </summary>
        /// <returns>JSON scheme of the configuration.</returns>
        public static string GenerateSchema()
        {
            return new JSchemaGenerator().Generate(typeof(Config)).ToString();
        }

        /// <summary>
        ///     Create a default configuration.
        /// </summary>
        /// <returns>Default configuration instance.</returns>
        public static Config CreateDefault()
        {
            return new Config
            {
                Debug = false,
                MailTo = ["ops@example.com"],
                Smtp = new SmtpSettings
                {
                    Host = "smtp.example.com",
                    Port = 587,
                    Username = "smtp-user",
                    Password = "smtp-password",
                    From = "copy@example.com",
                    EnableSsl = true
                },
                Clients =
                [
                    new Client
                    {
                        Type = ClientType.FTP,
                        Name = "FTP",
                        Host = "ftp.example.com",
                        Port = 21,
                        Username = "user",
                        Password = "password",
                        Fingerprint = "AA11BB22CC33DD44EE55FF6677889900AA11BB22"
                    },
                    new Client
                    {
                        Type = ClientType.SFTP,
                        Name = "SFTP",
                        Host = "sftp.example.com",
                        Port = 22,
                        Username = "user",
                        Password = "password"
                    },
                    new Client
                    {
                        Type = ClientType.SFTP,
                        Name = "SFTP_KEY",
                        Host = "sftp-key.example.com",
                        Port = 22,
                        Username = "user",
                        PrivateKey = "C:/keys/id_rsa",
                        Fingerprint = "11:22:33:44:55:66:77:88:99:AA:BB:CC:DD:EE:FF:00"
                    },
                    new Client { Type = ClientType.Local, Name = "Local", Host = "localhost" },
                    new Client { Type = ClientType.Local, Name = "Archive", Host = "localhost" },
                    new Client
                    {
                        Type = ClientType.Exchange,
                        Name = "Exchange",
                        Host = "https://exchange.example.com/EWS/Exchange.asmx",
                        Port = 443,
                        Username = "user@example.com",
                        Password = "password",
                        Autodiscover = true
                    }
                ],
                Tasks =
                [
                    new CopyTask
                    {
                        Id = "local-to-ftp-json",
                        Source = new CopyIO("Local", "C:/Copy/inbox"),
                        Destination = new CopyIO("FTP", "/incoming"),
                        Delete = true,
                        Filter = new CopyFilter
                        {
                            Name = "^.*\\.json$",
                            Author = ".*",
                            CreatedAfter = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Local),
                            MaxSize = 10485760,
                            MinSize = 1
                        }
                    },
                    new CopyTask
                    {
                        Id = "local-to-sftp-reports",
                        Source = new CopyIO("Local", "C:/Copy/outbox"),
                        Destination = new CopyIO("SFTP", "/upload"),
                        MoveOriginalTo = new CopyIO("Archive", "C:/Copy/archive"),
                        Filter = new CopyFilter
                        {
                            Name = "^report_.*\\.csv$",
                            Author = "^svc-copy$",
                            CreatedAfter = new DateTime(2024, 6, 1, 0, 0, 0, DateTimeKind.Local),
                            MaxSize = 52428800,
                            MinSize = 128
                        }
                    },
                    new CopyTask
                    {
                        Id = "local-to-ftp-zip",
                        Source = new CopyIO("Local", "C:/Copy/to-zip"),
                        Destination = new CopyIO("FTP", "destination"),
                        Delete = false,
                        Filter = new CopyFilter
                        {
                            Name = "^invoice_.*",
                            Author = ".*",
                            CreatedAfter = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Local),
                            MaxSize = 2097152,
                            MinSize = 0
                        },
                        Zip = true,
                        ZipFileName = "archive.zip"
                    }
                ]
            };
        }

        /// <summary>
        ///     Validate the configuration
        /// </summary>
        /// <exception cref="ConfigValidationException">Thrown when configuration is invalid.</exception>
        public void Validate()
        {
            List<string> validationErrors = GetValidationErrors();
            if (validationErrors.Count > 0)
            {
                throw new ConfigValidationException(validationErrors);
            }
        }

        /// <summary>
        ///     Collect all validation errors found in the configuration.
        /// </summary>
        /// <returns>List of validation errors.</returns>
        public List<string> GetValidationErrors()
        {
            List<string> validationErrors = [];

            if (Clients == null || Clients.Count == 0)
            {
                validationErrors.Add("La configuration doit contenir au moins un client");
            }

            foreach (Client client in Clients ?? [])
            {
                if (string.IsNullOrEmpty(client.Name))
                {
                    validationErrors.Add("Le nom du client ne peut pas être vide");
                }

                if (string.IsNullOrEmpty(client.Host))
                {
                    validationErrors.Add($"L'hôte du client {client.Name} ne peut pas être vide");
                }

                // Validation spécifique selon le type de client
                switch (client.Type)
                {
                    case ClientType.FTP:
                    case ClientType.Exchange:
                        if (string.IsNullOrEmpty(client.Username))
                        {
                            validationErrors.Add(
                                $"Le nom d'utilisateur est requis pour le client {client.Name} de type {client.Type}");
                        }

                        if (string.IsNullOrEmpty(client.Password))
                        {
                            validationErrors.Add($"Le mot de passe est requis pour le client {client.Name}");
                        }

                        break;
                    case ClientType.SFTP:
                        if (string.IsNullOrEmpty(client.Username))
                        {
                            validationErrors.Add(
                                $"Le nom d'utilisateur est requis pour le client {client.Name} de type {client.Type}");
                        }

                        if (string.IsNullOrEmpty(client.Password) && string.IsNullOrEmpty(client.PrivateKey))
                        {
                            validationErrors.Add(
                                $"Un mot de passe ou une clé privée est requis pour le client {client.Name}");
                        }

                        break;
                }
            }

            if (Tasks == null || Tasks.Count == 0)
            {
                validationErrors.Add("La configuration doit contenir au moins une tâche");
            }

            string[] normalizedRecipients = EmailNotifier.NormalizeRecipients(MailTo);
            MailTo = normalizedRecipients;

            foreach (string recipient in MailTo)
            {
                if (!EmailNotifier.IsValidEmailAddress(recipient))
                {
                    validationErrors.Add($"L'adresse email {recipient} est invalide");
                }
            }

            if (MailTo.Length > 0 && Smtp == null)
            {
                validationErrors.Add("MailTo nécessite une configuration SMTP pour envoyer les notifications");
            }

            if (Smtp != null)
            {
                if (string.IsNullOrWhiteSpace(Smtp.Host))
                {
                    validationErrors.Add("L'hôte SMTP ne peut pas être vide");
                }

                if (Smtp.Port <= 0)
                {
                    validationErrors.Add("Le port SMTP doit être supérieur à 0");
                }

                if (string.IsNullOrWhiteSpace(Smtp.From))
                {
                    validationErrors.Add("L'adresse From SMTP ne peut pas être vide");
                }
                else if (!EmailNotifier.IsValidEmailAddress(Smtp.From))
                {
                    validationErrors.Add($"L'adresse email SMTP From {Smtp.From} est invalide");
                }

                bool hasUsername = !string.IsNullOrWhiteSpace(Smtp.Username);
                bool hasPassword = !string.IsNullOrWhiteSpace(Smtp.Password);
                if (hasUsername != hasPassword)
                {
                    validationErrors.Add("SMTP Username et Password doivent être fournis ensemble");
                }
            }

            HashSet<string> clientNames = (Clients ?? []).Where(c => !string.IsNullOrEmpty(c.Name)).Select(c => c.Name)
                .ToHashSet()!;
            HashSet<string> taskIds = [];
            foreach (CopyTask task in Tasks ?? [])
            {
                if (task.Id != null)
                {
                    if (string.IsNullOrWhiteSpace(task.Id))
                    {
                        validationErrors.Add("L'identifiant de la tâche ne peut pas être vide");
                    }
                    else if (!taskIds.Add(task.Id))
                    {
                        validationErrors.Add($"L'identifiant de tâche {task.Id} est dupliqué");
                    }
                }

                if (task.Source == null)
                {
                    validationErrors.Add("La source de la tâche ne peut pas être vide");
                }
                else
                {
                    if (string.IsNullOrEmpty(task.Source.Client))
                    {
                        validationErrors.Add("La source de la tâche ne peut pas être vide");
                    }
                    else if (!clientNames.Contains(task.Source.Client))
                    {
                        validationErrors.Add($"Le client source {task.Source.Client} n'existe pas");
                    }
                }

                if (task.Destination == null)
                {
                    validationErrors.Add("La destination de la tâche ne peut pas être vide");
                }
                else
                {
                    if (string.IsNullOrEmpty(task.Destination.Client))
                    {
                        validationErrors.Add("La destination de la tâche ne peut pas être vide");
                    }
                    else if (!clientNames.Contains(task.Destination.Client))
                    {
                        validationErrors.Add($"Le client destination {task.Destination.Client} n'existe pas");
                    }
                }

                if (task.MoveOriginalTo != null && string.IsNullOrEmpty(task.MoveOriginalTo.Client))
                {
                    validationErrors.Add("Le client MoveOriginalTo ne peut pas être vide");
                }

                if (task.MoveOriginalTo != null && !clientNames.Contains(task.MoveOriginalTo.Client))
                {
                    validationErrors.Add(
                        $"Le client MoveOriginalTo {task.MoveOriginalTo.Client} n'existe pas");
                }

                if (task.Delete && task.MoveOriginalTo != null)
                {
                    validationErrors.Add(
                        "Une tâche ne peut pas utiliser Delete et MoveOriginalTo en même temps");
                }
            }

            return validationErrors;
        }
    }
}