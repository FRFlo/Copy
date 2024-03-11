using Copy.Clients;
using Copy.Types;

namespace Copy
{
    internal class Program
    {
        internal static Config Config = Config.Load(Config.ConfigPath);
        public static void Main(string[] args)
        {
            if (args.Length > 0)
            {
                switch (args[0])
                {
                    case "config":
                        Logger.Info("Creating default config and scheme files");
                        File.WriteAllText(Config.SchemePath, Config.GetScheme());
                        File.WriteAllText(Config.ConfigPath, Config.GetDefault());
                        return;
                    default:
                        Logger.Info($"Using config file {args[0]}");
                        Config.ConfigPath = args[0];
                        break;
                }
            }

            Dictionary<string, IClient> Clients = [];

            Logger.Info($"Connecting to {Config.Clients.Count} clients");

            foreach (Client client in Config.Clients)
            {
                Logger.Debug($"Creating client {client.Name} of type {client.Type}");
                switch (client.Type)
                {
                    case ClientType.FTP:
                        Clients.Add(client.Name, new FTP(client));
                        break;
                    case ClientType.SFTP:
                        Clients.Add(client.Name, new SFTP(client));
                        break;
                    case ClientType.Local:
                        Clients.Add(client.Name, new Local(client));
                        break;
                    case ClientType.Exchange:
                        Clients.Add(client.Name, new Exchange(client));
                        break;
                    default:
                        Logger.Error($"Client type {client.Type} unknown");
                        throw new ArgumentOutOfRangeException($"Client type {client.Type} unknown");
                }
            }

            Logger.Info($"Executing {Config.Tasks.Count} tasks");

            foreach (CopyTask task in Config.Tasks)
            {
                Logger.Info($"Copying files from {task.Source} to {task.Destination} using {task.Client}");
                if (!Clients.TryGetValue(task.Client, out IClient? client))
                {
                    Logger.Error($"Client {task.Source} not found");
                    throw new ClientNotFoundException($"Client {task.Source} not found");
                }

                string[] files = client.ListFiles(task.Source, task.Filter);

                Logger.Info($"Treating {files.Length} files");

                foreach (string file in files)
                {
                    string destination = Path.Combine(task.Destination, Path.GetFileName(file));
                    Logger.Debug($"Copying {file} to {task.Destination}");
                    Stream content = client.GetFile(file);
                    client.PutFile(destination, content);

                    if (task.Delete)
                    {
                        Logger.Debug($"Deleting {file}");
                        client.DeleteFile(file);
                    }

                    content.Close();

                    Logger.Debug($"Copied {file} to {task.Destination}");
                }

                Logger.Info($"Copied {files.Length} files from {task.Source} to {task.Destination} using {task.Client}");
            }

            Logger.Info("All tasks executed");
        }
    }
}
