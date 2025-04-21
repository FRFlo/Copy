using Copy.Clients;
using System.IO;

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
            var clients = new Dictionary<string, IClient>();
            
            foreach (var client in _config.Clients)
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

            foreach (var task in _config.Tasks)
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
            LoggerService.Info($"Copying files from {task.Source.Path} ({task.Source.Client}) to {task.Destination.Path} ({task.Destination.Client})");
            
            if (!_clients.TryGetValue(task.Source.Client, out var sourceClient))
            {
                throw new ClientNotFoundException($"Source client {task.Source.Client} not found");
            }
            
            if (!_clients.TryGetValue(task.Destination.Client, out var destinationClient))
            {
                throw new ClientNotFoundException($"Destination client {task.Destination.Client} not found");
            }

            var sourceFiles = sourceClient.ListFiles(task.Source.Path, task.Filter);
            
            foreach (var filePath in sourceFiles)
            {
                try 
                {
                    string fileName = Path.GetFileName(filePath);
                    string destPath = Path.Combine(task.Destination.Path, fileName);

                    sourceClient.CopyFile(filePath, destPath);

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
    }
}