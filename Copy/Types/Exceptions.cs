namespace Copy.Types
{
    /// <summary>
    /// Exception thrown when a client is not found.
    /// </summary>
    /// <param name="message">The message to display.</param>
    internal class ClientNotFoundException(string message) : Exception(message)
    {
    }
}
