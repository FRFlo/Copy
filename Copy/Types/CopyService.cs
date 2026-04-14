using Copy.Clients;

namespace Copy.Types
{
    internal class CopyService
    {
        private readonly Dictionary<string, IClient> _clients;
        private readonly Dictionary<string, string[]> _clientTaskMap;
        private readonly Config _config;

        public CopyService(Config config)
        {
            _config = config;
            _clientTaskMap = BuildClientTaskMap();
            _clients = InitializeClients();
        }

        private Dictionary<string, string[]> BuildClientTaskMap()
        {
            Dictionary<string, HashSet<string>> clientTaskMap = new(StringComparer.OrdinalIgnoreCase);

            for (int index = 0; index < _config.Tasks.Count; index++)
            {
                CopyTask task = _config.Tasks[index];
                string taskId = ResolveTaskId(task, index);
                RegisterTaskForClient(clientTaskMap, task.Source.Client, taskId);
                RegisterTaskForClient(clientTaskMap, task.Destination.Client, taskId);

                if (task.MoveOriginalTo != null)
                {
                    RegisterTaskForClient(clientTaskMap, task.MoveOriginalTo.Client, taskId);
                }
            }

            return clientTaskMap.ToDictionary(entry => entry.Key,
                entry => entry.Value.OrderBy(value => value).ToArray(),
                StringComparer.OrdinalIgnoreCase);
        }

        private Dictionary<string, IClient> InitializeClients()
        {
            Dictionary<string, IClient> clients = new();

            foreach (Client client in _config.Clients)
            {
                _clientTaskMap.TryGetValue(client.Name, out string[]? relatedTaskIds);
                using IDisposable scope = Logger.BeginScope(
                    ("phase", "client-init"),
                    ("client", client.Name),
                    ("clientType", client.Type.ToString()),
                    ("host", client.Host),
                    ("taskIds", relatedTaskIds is { Length: > 0 } ? string.Join(',', relatedTaskIds) : null));

                Logger.Info("Initializing client connection");

                IClient newClient = client.Type switch
                {
                    ClientType.FTP => new FTP(client),
                    ClientType.SFTP => new SFTP(client),
                    ClientType.Local => new Local(client),
                    ClientType.Exchange => new Exchange(client),
                    _ => throw new ArgumentOutOfRangeException($"Client type {client.Type} unknown")
                };

                Logger.Info("Client initialized successfully");
                clients.Add(client.Name, newClient);
            }

            return clients;
        }

        public async Task ExecuteTasksAsync()
        {
            Logger.Info($"Executing {_config.Tasks.Count} tasks");

            for (int index = 0; index < _config.Tasks.Count; index++)
            {
                CopyTask task = _config.Tasks[index];
                string taskId = ResolveTaskId(task, index);

                try
                {
                    await Task.Run(() => ExecuteSingleTask(task, taskId));
                }
                catch (Exception ex)
                {
                    using IDisposable scope = Logger.BeginScope(("taskId", taskId), ("phase", "task-execution"));
                    Logger.Error("Failed to execute task", ex);
                    // Continue with next task instead of throwing
                }
            }
        }

        private void ExecuteSingleTask(CopyTask task, string taskId)
        {
            using IDisposable scope = Logger.BeginScope(
                ("taskId", taskId),
                ("sourceClient", task.Source.Client),
                ("sourcePath", task.Source.Path),
                ("destinationClient", task.Destination.Client),
                ("destinationPath", task.Destination.Path));

            Logger.Info("Starting workflow execution");

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

            Logger.Debug("Listing source files");
            string[] sourceFiles = sourceClient.ListFiles(task.Source.Path, task.Filter);
            Logger.Info($"Workflow discovered {sourceFiles.Length} file(s) to process");

            int successCount = 0;
            int failureCount = 0;
            int skippedCount = 0;

            foreach (string filePath in sourceFiles)
            {
                try
                {
                    string fileName = Path.GetFileName(filePath);
                    string destPath = Path.Combine(task.Destination.Path, fileName);

                    using IDisposable fileScope = Logger.BeginScope(
                        ("file", fileName),
                        ("sourceFile", filePath),
                        ("destinationFile", destPath));

                    Logger.Info("Starting file workflow");

                    if (!task.Overwrite && destinationClient.DoFileExist(destPath))
                    {
                        Logger.Warn("Destination file already exists and overwrite is disabled; skipping file");
                        skippedCount++;
                        continue;
                    }

                    using Stream sourceStream = sourceClient.GetFile(filePath);
                    Logger.Debug("Source file stream opened successfully");
                    destinationClient.PutFile(destPath, sourceStream);
                    Logger.Info("File transferred successfully");

                    if (task.MoveOriginalTo != null)
                    {
                        MoveOriginalFile(task.MoveOriginalTo, moveOriginalClient!, sourceClient, filePath, fileName);
                    }

                    if (task.Delete)
                    {
                        sourceClient.DeleteFile(filePath);
                        Logger.Info("Source file deleted after successful transfer");
                    }

                    Logger.Info("File workflow completed successfully");
                    successCount++;
                }
                catch (Exception ex)
                {
                    failureCount++;
                    using IDisposable fileScope = Logger.BeginScope(("sourceFile", filePath));
                    Logger.Error("Failed to process file workflow", ex);
                    // Continue with next file
                }
            }

            Logger.Info(
                $"Workflow completed. Successful files: {successCount}. Failed files: {failureCount}. Skipped files: {skippedCount}.");
        }

        private static void MoveOriginalFile(CopyIO moveOriginalTo, IClient moveOriginalClient, IClient sourceClient,
            string sourcePath, string fileName)
        {
            string moveDestinationPath = Path.Combine(moveOriginalTo.Path, fileName);
            using IDisposable scope = Logger.BeginScope(
                ("moveOriginalClient", moveOriginalTo.Client),
                ("moveOriginalPath", moveDestinationPath));

            Logger.Info("Moving original file after successful transfer");

            if (ReferenceEquals(sourceClient, moveOriginalClient))
            {
                sourceClient.MoveFile(sourcePath, moveDestinationPath);
                Logger.Info("Original file moved within the same client");
                return;
            }

            using Stream sourceStream = sourceClient.GetFile(sourcePath);
            moveOriginalClient.PutFile(moveDestinationPath, sourceStream);
            sourceClient.DeleteFile(sourcePath);
            Logger.Info("Original file copied to archive client and deleted from source");
        }

        private static string ResolveTaskId(CopyTask task, int index)
        {
            return !string.IsNullOrWhiteSpace(task.Id)
                ? task.Id
                : $"task-{index + 1:000}";
        }

        private static void RegisterTaskForClient(Dictionary<string, HashSet<string>> clientTaskMap, string clientName,
            string taskId)
        {
            if (!clientTaskMap.TryGetValue(clientName, out HashSet<string>? taskIds))
            {
                taskIds = [];
                clientTaskMap[clientName] = taskIds;
            }

            taskIds.Add(taskId);
        }
    }
}