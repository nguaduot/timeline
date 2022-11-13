using Newtonsoft.Json;

namespace Timeline.Beans {
    public class StatsApiReq {
        [JsonProperty(PropertyName = "app")]
        public string App { set; get; }

        [JsonProperty(PropertyName = "pkg")]
        public string Package { set; get; }

        [JsonProperty(PropertyName = "ver")]
        public string Version { set; get; }

        [JsonProperty(PropertyName = "arch")]
        public string Architecture { set; get; }

        [JsonProperty(PropertyName = "api")]
        public string Api { set; get; }

        [JsonProperty(PropertyName = "dosageapp")]
        public int DosageApp { set; get; }

        [JsonProperty(PropertyName = "dosageapi")]
        public int DosageApi { set; get; }

        [JsonProperty(PropertyName = "os")]
        public string Os { set; get; }

        [JsonProperty(PropertyName = "osver")]
        public string OsVersion { set; get; }

        [JsonProperty(PropertyName = "screen")]
        public string Screen { set; get; }

        [JsonProperty(PropertyName = "device")]
        public string Device { set; get; }

        [JsonProperty(PropertyName = "devicename")]
        public string DeviceName { set; get; }

        [JsonProperty(PropertyName = "deviceid")]
        public string DeviceId { set; get; }

        [JsonProperty(PropertyName = "region")]
        public string Region { set; get; }
    }
}
