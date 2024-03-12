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
                Logger.Info($"Copying files from {task.Source.Path} ({task.Source.Client}) to {task.Destination.Path} ({task.Destination.Client})");
                if (!Clients.TryGetValue(task.Source.Client, out IClient? sourceClient))
                {
                    Logger.Error($"Source client {task.Source.Client} not found");
                    throw new ClientNotFoundException($"Source client {task.Source.Client} not found");
                }
                if (!Clients.TryGetValue(task.Destination.Client, out IClient? destinationClient))
                {
                    Logger.Error($"Destination client {task.Destination.Client} not found");
                    throw new ClientNotFoundException($"Destination client {task.Destination.Client} not found");
                }

                bool sameClient = task.Source.Client == task.Destination.Client;
                Logger.Debug($"Source and destination {(sameClient ? "are" : "aren't")} the same client.");

                ListResult[] files = sourceClient.ListFiles(task.Source.Path, task.Filter);

                Logger.Info($"Treating {files.Length} files");

                foreach (ListResult file in files)
                {
                    ListResult destination = new(task.Destination.Path, file.ElementName);
                    Logger.Debug($"Copying {file} to {task.Destination.Path}");

                    if (sameClient)
                    {
                        if (task.Delete)
                        {
                            Logger.Debug($"Moving {file} to {destination}");
                            sourceClient.MoveElement(file, destination);
                        }
                        else
                        {
                            Logger.Debug($"Copying {file} to {destination}");
                            sourceClient.CopyElement(file, destination);
                        }
                    }
                    else
                    {
                        Stream content = sourceClient.GetFile(file);
                        Logger.Debug($"Putting {file} ({task.Source.Client}) to {task.Destination.Path} ({destination})");
                        destinationClient.PutFile(destination, content);

                        if (task.Delete)
                        {
                            Logger.Debug($"Deleting {file}");
                            sourceClient.DeleteElement(file);
                        }

                        content.Close();
                    }
                }

                Logger.Info("All files treated");
            }

            Logger.Info("All tasks executed");
        }
    }
}
