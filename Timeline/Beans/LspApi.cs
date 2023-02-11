using Newtonsoft.Json;
using System.Collections.Generic;

namespace Timeline.Beans {
    public class LspApi {
        // 状态
        [JsonProperty(PropertyName = "status")]
        public int Status { set; get; }

        // 图片信息数组
        [JsonProperty(PropertyName = "data")]
        public List<LspApiData> Data { set; get; }
    }

    public class LspApiData : GeneralApiData {
        // 图集（可能为null）
        [JsonProperty(PropertyName = "album")]
        public string Album { set; get; }

        // 人物（可能为null）
        [JsonProperty(PropertyName = "character")]
        public string Character { set; get; }

        // R22
        [JsonProperty(PropertyName = "r22")]
        public int R22 { set; get; }
    }
}
