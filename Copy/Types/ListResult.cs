namespace Copy.Types
{
    /// <summary>
    /// Result of a list operation.
    /// </summary>
    /// <param name="location">Location of the file.</param>
    /// <param name="elementName">Name of the file.</param>
    internal class ListResult(string location, string elementName)
    {
        /// <summary>
        /// Location of the file.
        /// Folder path for filesystems, email name for email systems.
        /// </summary>
        public string Location { get; set; } = location;
        /// <summary>
        /// Name of the file.
        /// </summary>
        public string ElementName { get; set; } = elementName;

        public static ListResult FromPath(string path)
        {
            return new ListResult(
                Path.GetDirectoryName(path) ?? throw new ArgumentNullException($"Impossible to get directory from {path}"),
                Path.GetFileName(path) ?? throw new ArgumentNullException($"Impossible to get file name from {path}")
                );
        }
        public override string ToString()
        {
            return Path.Combine(Location, ElementName);
        }
    }
}