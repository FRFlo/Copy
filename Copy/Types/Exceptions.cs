namespace Copy.Types
{
    /// <summary>
    /// Exception thrown when a client is not found.
    /// </summary>
    /// <param name="message">The message to display.</param>
    internal class ClientNotFoundException(string message) : Exception(message)
    {
    }

    /// <summary>
    /// Exception thrown when a file owner is not found.
    /// </summary>
    /// <param name="message">The message to display.</param>
    internal class FileOwnerNotFoundException(string message) : Exception(message)
    {
    }
}
