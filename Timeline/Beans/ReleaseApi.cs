using Newtonsoft.Json;
using System;

namespace Timeline.Beans {
    public class ReleaseApi {
        public string Version { get; set; }

        public string Url { get; set; }
    }

    public class AppstatsApi {
        [JsonProperty(PropertyName = "status")]
        public int Status { set; get; }

        [JsonProperty(PropertyName = "data")]
        public AppstatsApiData Data { set; get; }
    }

    public class AppstatsApiData {
        // 版本
        [JsonProperty(PropertyName = "ver")]

        public string Version { get; set; }

        // 链接
        [JsonProperty(PropertyName = "url")]
        public string Url { get; set; }
    }
}
