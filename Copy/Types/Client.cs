using Newtonsoft.Json;
using System.Runtime.Serialization;

namespace Copy.Types
{
    /// <summary>
    /// Type of client used to copy files.
    /// </summary>
    internal enum ClientType
    {
        [EnumMember(Value = "FTP")]
        FTP,
        [EnumMember(Value = "SFTP")]
        SFTP,
        [EnumMember(Value = "Local")]
        Local,
        [EnumMember(Value = "Exchange")]
        Exchange
    }
    /// <summary>
    /// Client used to copy files.
    /// </summary>
    internal class Client
    {
        private int? _port = null;
        /// <summary>
        /// Type of client.
        /// </summary>
        [JsonProperty(PropertyName = "Type", Required = Required.Always)]
        public required ClientType Type { get; set; }
        /// <summary>
        /// Name of the client.
        /// </summary>
        [JsonProperty(PropertyName = "Name", Required = Required.Always)]
        public required string Name { get; set; }
        /// <summary>
        /// Host of the client.
        /// </summary>
        [JsonProperty(PropertyName = "Host", Required = Required.Always)]
        public required string Host { get; set; }
        /// <summary>
        /// Port of the client.
        /// </summary>
        [JsonProperty(PropertyName = "Port", Required = Required.DisallowNull)]
        public int Port
        {
            get => _port ?? (Type switch
            {
                ClientType.FTP => 21,
                ClientType.SFTP => 22,
                ClientType.Exchange => 443,
                _ => 0
            });
            set => _port = value;
        }
        /// <summary>
        /// Username of the client.
        /// </summary>
        [JsonProperty(PropertyName = "Username", Required = Required.Default)]
        public string? Username { get; set; }
        /// <summary>
        /// Password of the client.
        /// </summary>
        [JsonProperty(PropertyName = "Password", Required = Required.Default)]
        public string? Password { get; set; }
        /// <summary>
        /// Private key of the client.
        /// </summary>
        [JsonProperty(PropertyName = "PrivateKey", Required = Required.Default)]
        public string? PrivateKey { get; set; }
        /// <summary>
        /// Fingerprint of the server.
        /// </summary>
        [JsonProperty(PropertyName = "Fingerprint", Required = Required.Default)]
        public string? Fingerprint { get; set; }
        /// <summary>
        /// Exchange autodiscover server.
        /// </summary>
        [JsonProperty(PropertyName = "Autodiscover", Required = Required.Default)]
        public bool Autodiscover { get; set; } = false;
    }
}
