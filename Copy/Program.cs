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
                if (!string.IsNullOrWhiteSpace(runId))
                {
                    Console.Error.WriteLine($"RunId: {runId}");
                }

                foreach (string error in ex.Errors)
                {
                    Console.Error.WriteLine($"- {error}");
                }

                Environment.Exit(1);
            }
            catch (Exception ex)
            {
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
    }
}