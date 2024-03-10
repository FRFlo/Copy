using Newtonsoft.Json;

namespace Copy.Types
{
    /// <summary>
    /// Task to copy files.
    /// </summary>
    public class CopyTask
    {
        /// <summary>
        /// Client to use for the task.
        /// </summary>
        [JsonProperty(PropertyName = "Client", Required = Required.Always)]
        public required string Client { get; set; }
        /// <summary>
        /// Source of files to copy.
        /// </summary>
        [JsonProperty(PropertyName = "Source", Required = Required.Always)]
        public required string Source { get; set; }
        /// <summary>
        /// Destination of files to copy.
        /// </summary>
        [JsonProperty(PropertyName = "Destination", Required = Required.Always)]
        public required string Destination { get; set; }
        /// <summary>
        /// Whether to delete files after copying.
        /// </summary>
        [JsonProperty(PropertyName = "Delete", Required = Required.DisallowNull)]
        public bool Delete { get; set; } = false;
        /// <summary>
        /// Filter for files to copy.
        /// </summary>
        [JsonProperty(PropertyName = "Filter", Required = Required.DisallowNull)]
        public string Filter { get; set; } = ".*";
    }
}