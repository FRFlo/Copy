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
                if (args.Length > 0 && args[0] == "config")
                {
                    HandleConfigGeneration();
                    return;
                }

                string configPath = args.Length > 0 ? args[0] : DefaultConfigPath;
                var config = Config.FromFile(configPath);
                
                // Initialize logging
                LoggerService.Initialize(config.Debug);

                var copyService = new CopyService(config);
                await copyService.ExecuteTasksAsync();
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
            
            var defaultConfig = Config.CreateDefault();
            File.WriteAllText(DefaultSchemePath, Config.GenerateSchema());
            File.WriteAllText(DefaultConfigPath, JsonConvert.SerializeObject(defaultConfig, Formatting.Indented));
            
            LoggerService.Dispose();
        }
    }
}
