using Newtonsoft.Json;
using System.Collections.Generic;

namespace Timeline.Beans {
    public class YmyouliApi {
        // 状态
        [JsonProperty(PropertyName = "status")]
        public int Status { set; get; }

        // 图片信息数组
        [JsonProperty(PropertyName = "data")]
        public List<YmyouliApiData> Data { set; get; }
    }

    public class YmyouliApiData : GeneralApiData {
        // 图集（可能为null）
        [JsonProperty(PropertyName = "album")]
        public string Album { set; get; }
    }
}
