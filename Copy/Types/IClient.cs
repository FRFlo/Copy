namespace Copy.Types
{
    /// <summary>
    /// Interface for a client used to copy, delete, and list files.
    /// </summary>
    internal interface IClient : IDisposable
    {
        /// <summary>
        /// Client configuration.
        /// </summary>
        Client Config { get; }
        /// <summary>
        /// List files in a directory.
        /// </summary>
        /// <param name="path">Path to the directory.</param>
        /// <param name="filter">Filter for files to list.</param>
        /// <returns>Array of file names.</returns>
        ListResult[] ListFiles(string path, CopyFilter filter);
        /// <summary>
        /// Check if a element exists.
        /// </summary>
        /// <param name="element">Element.</param>
        /// <returns>True if the element exists, false otherwise.</returns>
        bool DoFileExist(ListResult element);
        /// <summary>
        /// Get a file.
        /// </summary>
        /// <param name="element">Element.</param>
        /// <returns>Stream of the file.</returns>
        Stream GetFile(ListResult element);
        /// <summary>
        /// Put a file.
        /// </summary>
        /// <param name="element">Element.</param>
        /// <param name="stream">Stream of the file.</param>
        void PutFile(ListResult element, Stream stream);
        /// <summary>
        /// Move a element.
        /// </summary>
        /// <param name="sourceElement">The source element.</param>
        /// <param name="destinationElement">The destination element.</param>
        void MoveElement(ListResult sourceElement, ListResult destinationElement);
        /// <summary>
        /// Copy a element.
        /// </summary>
        /// <param name="sourceElement">Path to the source file.</param>
        /// <param name="destinationElement">Path to the destination file.</param>
        void CopyElement(ListResult sourceElement, ListResult destinationElement);
        /// <summary>
        /// Delete a element.
        /// </summary>
        /// <param name="element">Element.</param>
        void DeleteElement(ListResult element);
    }
}
