using Newtonsoft.Json;
using System.Collections.Generic;

namespace Timeline.Beans {
    public class TimelineApi {
        // 状态
        [JsonProperty(PropertyName = "status")]
        public int Status { set; get; }

        // 图片信息数组
        [JsonProperty(PropertyName = "data")]
        public List<TimelineApiData> Data { set; get; }
    }

    public class TimelineApiData : GeneralApiData {
        // 发布平台
        [JsonProperty(PropertyName = "platform")]
        public string Platform { set; get; }

        // 未授权
        [JsonProperty(PropertyName = "unauthorized")]
        public int Unauthorized { set; get; }
    }
}
