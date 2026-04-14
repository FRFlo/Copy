using Newtonsoft.Json;

namespace Copy.Types
{
    internal class SmtpSettings
    {
        [JsonProperty(nameof(Host), Required = Required.DisallowNull)]
        public string Host { get; set; } = string.Empty;

        [JsonProperty(nameof(Port), Required = Required.DisallowNull)]
        public int Port { get; set; } = 587;

        [JsonProperty(nameof(Username), Required = Required.Default)]
        public string? Username { get; set; }

        [JsonProperty(nameof(Password), Required = Required.Default)]
        public string? Password { get; set; }

        [JsonProperty(nameof(From), Required = Required.DisallowNull)]
        public string From { get; set; } = string.Empty;

        [JsonProperty(nameof(EnableSsl), Required = Required.DisallowNull)]
        public bool EnableSsl { get; set; } = true;
    }
}