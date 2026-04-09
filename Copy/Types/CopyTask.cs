using Newtonsoft.Json;

namespace Copy.Types
{
    /// <summary>
    ///     Task to copy files.
    /// </summary>
    public class CopyTask
    {
        /// <summary>
        ///     Source of files to copy.
        /// </summary>
        [JsonProperty(PropertyName = "Source", Required = Required.Always)]
        public required CopyIO Source { get; set; }

        /// <summary>
        ///     Destination of files to copy.
        /// </summary>
        [JsonProperty(PropertyName = "Destination", Required = Required.Always)]
        public required CopyIO Destination { get; set; }

        /// <summary>
        ///     Whether to delete files after copying.
        /// </summary>
        [JsonProperty(PropertyName = "Delete", Required = Required.DisallowNull)]
        public bool Delete { get; set; }

        /// <summary>
        ///     Optional location where the original file should be moved after a successful copy.
        /// </summary>
        [JsonProperty(PropertyName = "MoveOriginalTo", Required = Required.Default,
            NullValueHandling = NullValueHandling.Ignore)]
        public CopyIO? MoveOriginalTo { get; set; }

        /// <summary>
        ///     Filter for files to copy.
        /// </summary>
        [JsonProperty(PropertyName = "Filter", Required = Required.DisallowNull)]
        public CopyFilter Filter { get; set; } = new();

        /// <summary>
        ///     Whether to zip the files when copying.
        /// </summary>
        [JsonProperty(PropertyName = "Zip", Required = Required.DisallowNull)]
        public bool Zip { get; set; }

        /// <summary>
        ///     Custom name for the zip file. If not specified and Zip is true,
        ///     a default name based on the timestamp will be used.
        /// </summary>
        [JsonProperty(PropertyName = "ZipFileName", NullValueHandling = NullValueHandling.Ignore)]
        public string? ZipFileName { get; set; }
    }
}