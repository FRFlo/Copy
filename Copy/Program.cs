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

                string configPath = args.Length > 0 ? args[0] : DefaultConfigPath;
                Config config = Config.FromFile(configPath);

                // Initialize logging
                LoggerService.Initialize(config.Debug);

                CopyService copyService = new(config);
                await copyService.ExecuteTasksAsync();
            }
            catch (ConfigValidationException ex)
            {
                foreach (string error in ex.Errors)
                {
                    Console.Error.WriteLine($"- {error}");
                }

                Environment.Exit(1);
            }
            catch (Exception ex)
            {
                Logger.Error($"Application error: {ex.Message}");
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
