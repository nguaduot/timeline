using Newtonsoft.Json;
using System.Collections.Generic;

namespace TimelineService.Beans {
    public sealed class Himawari8Api {
        // 最新图UTC时间
        [JsonProperty(PropertyName = "date")]
        public string Date { set; get; }
    }
}
