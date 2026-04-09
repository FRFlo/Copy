using Copy.Clients;

namespace Copy.Types
{
    internal class CopyService
    {
        private readonly Dictionary<string, IClient> _clients;
        private readonly Config _config;

        public CopyService(Config config)
        {
            _config = config;
            _clients = InitializeClients();
        }

        private Dictionary<string, IClient> InitializeClients()
        {
            Dictionary<string, IClient> clients = new();

            foreach (Client client in _config.Clients)
            {
                Logger.Debug($"Creating client {client.Name} of type {client.Type}");
                IClient newClient = client.Type switch
                {
                    ClientType.FTP => new FTP(client),
                    ClientType.SFTP => new SFTP(client),
                    ClientType.Local => new Local(client),
                    ClientType.Exchange => new Exchange(client),
                    _ => throw new ArgumentOutOfRangeException($"Client type {client.Type} unknown")
                };
                clients.Add(client.Name, newClient);
            }

            return clients;
        }

        public async Task ExecuteTasksAsync()
        {
            Logger.Info($"Executing {_config.Tasks.Count} tasks");

            foreach (CopyTask task in _config.Tasks)
            {
                try
                {
                    await Task.Run(() => ExecuteSingleTask(task));
                }
                catch (Exception ex)
                {
                    Logger.Error($"Failed to execute task: {ex.Message}");
                    // Continue with next task instead of throwing
                }
            }
        }

        private void ExecuteSingleTask(CopyTask task)
        {
            LoggerService.Info(
                $"Copying files from {task.Source.Path} ({task.Source.Client}) to {task.Destination.Path} ({task.Destination.Client})");

            if (!_clients.TryGetValue(task.Source.Client, out IClient? sourceClient))
            {
                throw new ClientNotFoundException($"Source client {task.Source.Client} not found");
            }

            if (!_clients.TryGetValue(task.Destination.Client, out IClient? destinationClient))
            {
                throw new ClientNotFoundException($"Destination client {task.Destination.Client} not found");
            }

            IClient? moveOriginalClient = null;
            if (task.MoveOriginalTo != null)
            {
                if (!_clients.TryGetValue(task.MoveOriginalTo.Client, out moveOriginalClient))
                {
                    throw new ClientNotFoundException($"MoveOriginalTo client {task.MoveOriginalTo.Client} not found");
                }
            }

            string[] sourceFiles = sourceClient.ListFiles(task.Source.Path, task.Filter);

            foreach (string filePath in sourceFiles)
            {
                try
                {
                    string fileName = Path.GetFileName(filePath);
                    string destPath = Path.Combine(task.Destination.Path, fileName);

                    using Stream sourceStream = sourceClient.GetFile(filePath);
                    destinationClient.PutFile(destPath, sourceStream);

                    if (task.MoveOriginalTo != null)
                    {
                        MoveOriginalFile(task.MoveOriginalTo, moveOriginalClient!, sourceClient, filePath, fileName);
                    }

                    if (task.Delete)
                    {
                        sourceClient.DeleteFile(filePath);
                    }
                }
                catch (Exception ex)
                {
                    LoggerService.Error($"Failed to copy file {filePath}: {ex.Message}");
                    // Continue with next file
                }
            }
        }

        private static void MoveOriginalFile(CopyIO moveOriginalTo, IClient moveOriginalClient, IClient sourceClient,
            string sourcePath, string fileName)
        {
            string moveDestinationPath = Path.Combine(moveOriginalTo.Path, fileName);

            if (ReferenceEquals(sourceClient, moveOriginalClient))
            {
                sourceClient.MoveFile(sourcePath, moveDestinationPath);
                return;
            }

            using Stream sourceStream = sourceClient.GetFile(sourcePath);
            moveOriginalClient.PutFile(moveDestinationPath, sourceStream);
            sourceClient.DeleteFile(sourcePath);
        }
    }
}