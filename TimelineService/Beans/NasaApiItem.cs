using Newtonsoft.Json;

namespace TimelineService.Beans {
    public sealed class NasaApiItem {
        // 媒体类型
        [JsonProperty(PropertyName = "media_type")]
        public string MediaType { set; get; }

        // 原图URL（media_type为“video”时缺失）
        [JsonProperty(PropertyName = "hdurl")]
        public string HdUrl { set; get; }

        // 缩略图URL（media_type为“video”时是视频链接）
        [JsonProperty(PropertyName = "url")]
        public string Url { set; get; }
    }
}
