using Copy.Types;
using Newtonsoft.Json;

namespace Copy
{
    internal class Program
    {
        private const string DefaultConfigPath = "config.json";
        private const string DefaultSchemePath = "scheme.json";

        public static async Task Main(string[] args)
        {
            string? runId = null;
            string? configPath = null;

            try
            {
                if (args.Length > 0)
                {
                    switch (args[0].ToLowerInvariant())
                    {
                        case "config":
                            HandleConfigGeneration();
                            return;
                        case "validate":
                            HandleConfigValidation(args.Length > 1 ? args[1] : DefaultConfigPath);
                            return;
                    }
                }

                configPath = args.Length > 0 ? args[0] : DefaultConfigPath;
                Config config = Config.FromFile(configPath);
                runId = $"run-{Guid.NewGuid():N}";

                // Initialize logging
                LoggerService.Initialize(config.Debug);

                using IDisposable scope = Logger.BeginScope(("runId", runId));
                Logger.ConfigureNotifications(config.Smtp, config.MailTo);
                Logger.Info($"Configuration loaded successfully from '{configPath}'");

                CopyService copyService = new(config);
                await copyService.ExecuteTasksAsync();
                Logger.Info("Application run completed successfully");
            }
            catch (ConfigValidationException ex)
            {
                ConfigureNotificationsForStartupFailures(configPath);

                if (!string.IsNullOrWhiteSpace(runId))
                {
                    Console.Error.WriteLine($"RunId: {runId}");
                }

                using IDisposable scope = Logger.BeginScope(("runId", runId), ("configPath", configPath));
                Logger.Error("Configuration validation failed", ex);

                foreach (string error in ex.Errors)
                {
                    Console.Error.WriteLine($"- {error}");
                }

                Environment.Exit(1);
            }
            catch (Exception ex)
            {
                ConfigureNotificationsForStartupFailures(configPath);
                using IDisposable scope = Logger.BeginScope(("runId", runId));
                Logger.Error("Application error", ex);
                Environment.Exit(1);
            }
            finally
            {
                LoggerService.Dispose();
            }
        }

        private static void HandleConfigGeneration()
        {
            LoggerService.Initialize();
            LoggerService.Info("Creating default config and scheme files");

            Config defaultConfig = Config.CreateDefault();
            File.WriteAllText(DefaultSchemePath, Config.GenerateSchema());
            File.WriteAllText(DefaultConfigPath, JsonConvert.SerializeObject(defaultConfig, Formatting.Indented));

            LoggerService.Dispose();
        }

        private static void HandleConfigValidation(string configPath)
        {
            Config.FromFile(configPath);
            Console.WriteLine($"Configuration '{configPath}' is valid.");
        }

        private static void ConfigureNotificationsForStartupFailures(string? configPath)
        {
            try
            {
                string[] recipients = [];
                SmtpSettings? smtp = null;

                if (!string.IsNullOrWhiteSpace(configPath) && File.Exists(configPath))
                {
                    JsonSerializerSettings serializerSettings = new();
                    serializerSettings.Error += (_, args) => { args.ErrorContext.Handled = true; };

                    Config? config = JsonConvert.DeserializeObject<Config>(File.ReadAllText(configPath), serializerSettings);
                    recipients = EmailNotifier.NormalizeRecipients(config?.MailTo)
                        .Where(EmailNotifier.IsValidEmailAddress)
                        .ToArray();
                    smtp = config?.Smtp;
                }

                bool hasSmtpEnvironmentOverride =
                    !string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("COPY_SMTP_HOST")) ||
                    !string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("COPY_SMTP_USERNAME")) ||
                    !string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("COPY_SMTP_PASSWORD")) ||
                    !string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("COPY_SMTP_FROM")) ||
                    !string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("COPY_SMTP_PORT")) ||
                    !string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("COPY_SMTP_ENABLESSL"));

                if (smtp == null && hasSmtpEnvironmentOverride)
                {
                    smtp = new SmtpSettings();
                }

                if (smtp != null)
                {
                    string? smtpHost = Environment.GetEnvironmentVariable("COPY_SMTP_HOST");
                    if (!string.IsNullOrWhiteSpace(smtpHost))
                    {
                        smtp.Host = smtpHost;
                    }

                    string? smtpUsername = Environment.GetEnvironmentVariable("COPY_SMTP_USERNAME");
                    if (!string.IsNullOrWhiteSpace(smtpUsername))
                    {
                        smtp.Username = smtpUsername;
                    }

                    string? smtpPassword = Environment.GetEnvironmentVariable("COPY_SMTP_PASSWORD");
                    if (!string.IsNullOrWhiteSpace(smtpPassword))
                    {
                        smtp.Password = smtpPassword;
                    }

                    string? smtpFrom = Environment.GetEnvironmentVariable("COPY_SMTP_FROM");
                    if (!string.IsNullOrWhiteSpace(smtpFrom))
                    {
                        smtp.From = smtpFrom;
                    }

                    if (int.TryParse(Environment.GetEnvironmentVariable("COPY_SMTP_PORT"), out int smtpPort))
                    {
                        smtp.Port = smtpPort;
                    }

                    if (bool.TryParse(Environment.GetEnvironmentVariable("COPY_SMTP_ENABLESSL"), out bool smtpEnableSsl))
                    {
                        smtp.EnableSsl = smtpEnableSsl;
                    }
                }

                if (smtp != null &&
                    !string.IsNullOrWhiteSpace(smtp.Host) &&
                    smtp.Port > 0 &&
                    !string.IsNullOrWhiteSpace(smtp.From) &&
                    EmailNotifier.IsValidEmailAddress(smtp.From))
                {
                    Logger.ConfigureNotifications(smtp, recipients);
                }
            }
            catch
            {
                // Best-effort startup notification bootstrap: never hide the original failure.
            }
        }
    }
}
