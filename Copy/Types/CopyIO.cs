using Newtonsoft.Json;

namespace Copy.Types
{
    /// <summary>
    /// Input/output for copying files.
    /// </summary>
    public class CopyIO
    {
        /// <summary>
        /// Create a new instance of the class.
        /// </summary>
        /// <param name="client">Client to use.</param>
        /// <param name="path">Path to the folder.</param>
        public CopyIO(string client, string path)
        {
            Client = client;
            Path = path;
        }

        /// <summary>
        /// Client to use.
        /// </summary>
        [JsonProperty(PropertyName = "Client", Required = Required.Always)]
        public string Client { get; set; }
        /// <summary>
        /// Path to the folder.
        /// </summary>
        [JsonProperty(PropertyName = "Path", Required = Required.Always)]
        public string Path { get; set; }
    }
}
