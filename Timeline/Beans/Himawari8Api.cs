using Newtonsoft.Json;
using System.Collections.Generic;

namespace Timeline.Beans {
    public class Himawari8Api {
        // 最新时间
        [JsonProperty(PropertyName = "date")]
        public string Date { set; get; }

        // 图片链接
        [JsonProperty(PropertyName = "file")]
        public string File { set; get; }
    }
}
