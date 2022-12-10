using Newtonsoft.Json;
using System.Collections.Generic;

namespace TimelineService.Beans {
    public sealed class IhansenApi {
        // 图片宽度
        [JsonProperty(PropertyName = "width")]
        public int Width { set; get; }

        // 图片高度
        [JsonProperty(PropertyName = "height")]
        public int Height { set; get; }

        // 图片URL
        [JsonProperty(PropertyName = "raw")]
        public string Raw { set; get; }

        // 缩略图URL
        [JsonProperty(PropertyName = "smallUrl")]
        public string SmallUrl { set; get; }
    }
}
