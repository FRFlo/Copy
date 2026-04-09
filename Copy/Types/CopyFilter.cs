using Newtonsoft.Json;

namespace Copy.Types
{
    /// <summary>
    ///     Filter for files to copy.
    /// </summary>
    public class CopyFilter
    {
        private DateTime _createdAfter = DateTime.SpecifyKind(DateTime.MinValue, DateTimeKind.Local);

        /// <summary>
        ///     Pattern the name of the files must match.
        /// </summary>
        [JsonProperty(PropertyName = "Name", Required = Required.DisallowNull)]
        public string Name { get; set; } = ".*";

        /// <summary>
        ///     Pattern the author of the files must match.
        /// </summary>
        [JsonProperty(PropertyName = "Author", Required = Required.DisallowNull)]
        public string Author { get; set; } = ".*";

        /// <summary>
        ///     Date the files must be created after.
        /// </summary>
        [JsonProperty(PropertyName = "CreatedAfter", Required = Required.DisallowNull)]
        public DateTime CreatedAfter
        {
            get => _createdAfter;
            set => _createdAfter = value.Kind switch
            {
                DateTimeKind.Utc => value.ToLocalTime(),
                DateTimeKind.Local => value,
                _ => DateTime.SpecifyKind(value, DateTimeKind.Local)
            };
        }

        /// <summary>
        ///     Size the files must not exceed.
        /// </summary>
        [JsonProperty(PropertyName = "MaxSize", Required = Required.DisallowNull)]
        public ulong MaxSize { get; set; } = ulong.MaxValue;

        /// <summary>
        ///     Size the files must exceed.
        /// </summary>
        [JsonProperty(PropertyName = "MinSize", Required = Required.DisallowNull)]
        public ulong MinSize { get; set; }
    }
}