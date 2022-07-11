using Newtonsoft.Json;
using System.Collections.Generic;

namespace Timeline.Beans {
    public class LifeApi {
        // 状态
        [JsonProperty(PropertyName = "status")]
        public int Status { set; get; }

        // 结果
        [JsonProperty(PropertyName = "data")]
        public LifeApiData Data { set; get; }
    }

    public class LifeApiData {
        // 历史成本
        [JsonProperty(PropertyName = "cost")]
        public float Cost { set; get; }

        // 当前日均成本
        [JsonProperty(PropertyName = "costdaily")]
        public float CostDaily { set; get; }

        // 历史赞助
        [JsonProperty(PropertyName = "donate")]
        public float Donate { set; get; }

        // 历史赞助笔数
        [JsonProperty(PropertyName = "donatecount")]
        public int DonateCount { set; get; }

        // 已运营天数
        [JsonProperty(PropertyName = "past")]
        public int Past { set; get; }

        // 可运营天数
        [JsonProperty(PropertyName = "remain")]
        public int Remain { set; get; }
    }
}
