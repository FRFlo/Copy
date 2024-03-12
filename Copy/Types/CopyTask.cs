using Newtonsoft.Json;

namespace Copy.Types
{
    /// <summary>
    /// Task to copy files.
    /// </summary>
    public class CopyTask
    {
        /// <summary>
        /// Source of files to copy.
        /// </summary>
        [JsonProperty(PropertyName = "Source", Required = Required.Always)]
        public required CopyIO Source { get; set; }
        /// <summary>
        /// Destination of files to copy.
        /// </summary>
        [JsonProperty(PropertyName = "Destination", Required = Required.Always)]
        public required CopyIO Destination { get; set; }
        /// <summary>
        /// Whether to delete files after copying.
        /// </summary>
        [JsonProperty(PropertyName = "Delete", Required = Required.DisallowNull)]
        public bool Delete { get; set; } = false;
        /// <summary>
        /// Filter for files to copy.
        /// </summary>
        [JsonProperty(PropertyName = "Filter", Required = Required.DisallowNull)]
        public CopyFilter Filter { get; set; } = new();
    }
}