using Newtonsoft.Json;

namespace GHubBatteryStatusPlugin.Internal
{
    internal class GHubBatteryStats
    {
        [JsonProperty(PropertyName = "isCharging")]
        public bool IsCharging { get; set; }

        [JsonProperty(PropertyName = "isConnected")]
        public bool IsConnected { get; set; }

        [JsonProperty(PropertyName = "millivolts")]
        public int Millivolts { get; set; }

        [JsonProperty(PropertyName = "percentage")]
        public double Percentage { get; set; }
    }
}
