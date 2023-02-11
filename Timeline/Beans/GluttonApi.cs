using Newtonsoft.Json;
using System.Collections.Generic;

namespace Timeline.Beans {
    public class GluttonApi {
        // 状态
        [JsonProperty(PropertyName = "status")]
        public int Status { set; get; }

        // 图片信息数组
        [JsonProperty(PropertyName = "data")]
        public List<GluttonApiData> Data { set; get; }
    }

    public class GluttonApiData : GeneralApiData {
        // 期数（仅限周选集）
        [JsonProperty(PropertyName = "phase")]
        public int Phase { set; get; }

        // 原图源ID
        [JsonProperty(PropertyName = "rawprovider")]
        public string RawProvider { set; get; }

        // 原图源图片ID
        [JsonProperty(PropertyName = "rawid")]
        public string RawId { set; get; }
    }
}
