using Newtonsoft.Json;
using System.Collections.Generic;

namespace TimelineService.Beans {
    public sealed class OneplusApi {
        // 图片信息数组
        [JsonProperty(PropertyName = "data")]
        public IList<OneplusApiItem> Items { set; get; }
    }

    public sealed class OneplusApiItem {
        // URL
        [JsonProperty(PropertyName = "photoUrl")]
        public string PhotoUrl { set; get; }
    }

    public sealed class OneplusRequest {
        [JsonProperty(PropertyName = "pageSize")]
        public int PageSize { set; get; }

        [JsonProperty(PropertyName = "currentPage")]
        public int CurrentPage { set; get; }

        [JsonProperty(PropertyName = "sortMethod")]
        public string SortMethod { set; get; }
    }
}
