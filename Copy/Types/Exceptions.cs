namespace Copy.Types
{
    /// <summary>
    ///     Exception thrown when a client is not found.
    /// </summary>
    /// <param name="message">The message to display.</param>
    internal class ClientNotFoundException(string message) : Exception(message)
    {
    }

    /// <summary>
    ///     Exception thrown when a file owner is not found.
    /// </summary>
    /// <param name="message">The message to display.</param>
    internal class FileOwnerNotFoundException(string message) : Exception(message)
    {
    }

    /// <summary>
    ///     Exception thrown when configuration validation finds one or more errors.
    /// </summary>
    internal class ConfigValidationException : Exception
    {
        public ConfigValidationException(IReadOnlyList<string> errors)
            : base($"Configuration validation failed:{Environment.NewLine}- {string.Join(Environment.NewLine + "- ", errors)}")
        {
            Errors = errors;
        }

        /// <summary>
        ///     Validation errors collected while reading the configuration.
        /// </summary>
        public IReadOnlyList<string> Errors { get; }
    }
}
