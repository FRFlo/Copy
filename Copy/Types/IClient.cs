namespace Copy.Types
{
    /// <summary>
    /// Interface for a client used to copy, delete, and list files.
    /// </summary>
    internal interface IClient : IDisposable
    {
        /// <summary>
        /// Client credentials.
        /// </summary>
        Client Credentials { get; }
        /// <summary>
        /// List files in a directory.
        /// </summary>
        /// <param name="path">Path to the directory.</param>
        /// <returns>Array of file names.</returns>
        string[] ListFiles(string path);
        /// <summary>
        /// Check if a file exists.
        /// </summary>
        /// <param name="path">Path to the directory.</param>
        /// <returns>True if the file exists, false otherwise.</returns>
        bool DoFileExist(string path);
        /// <summary>
        /// Get a file.
        /// </summary>
        /// <param name="path">Path to the directory.</param>
        /// <returns>Stream of the file.</returns>
        Stream GetFile(string path);
        /// <summary>
        /// Put a file.
        /// </summary>
        /// <param name="path">Path to the directory.</param>
        /// <param name="stream">Stream of the file.</param>
        void PutFile(string path, Stream stream);
        /// <summary>
        /// Delete a file.
        /// </summary>
        /// <param name="path">Path to the directory.</param>
        void DeleteFile(string path);
    }
}
