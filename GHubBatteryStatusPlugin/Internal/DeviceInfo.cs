using Newtonsoft.Json;

namespace GHubBatteryStatusPlugin.Internal
{
    internal class DeviceInfo
    {
        [JsonProperty(PropertyName = "name")]
        public string Name { get; set; }
    }
}
