using Newtonsoft.Json;
using System.Collections.Generic;

namespace Timeline.Beans {
    public class R22AuthApi {
        // 状态
        [JsonProperty(PropertyName = "status")]
        public int Status { set; get; }

        // R22授权结果
        [JsonProperty(PropertyName = "data")]
        public R22AuthApiData Data { set; get; }
    }

    public class R22AuthApiData {
        // 设备ID
        [JsonProperty(PropertyName = "deviceid")]
        public string DeviceId { set; get; }

        // 授权结果
        [JsonProperty(PropertyName = "r22")]
        public int R22 { set; get; }

        // 暗号
        [JsonProperty(PropertyName = "comment")]
        public string Comment { set; get; }
    }
}
