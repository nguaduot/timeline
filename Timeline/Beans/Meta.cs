using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System;
using System.Collections.Generic;
using System.Drawing;
using Windows.Networking.BackgroundTransfer;
using Windows.Storage;

namespace Timeline.Beans {
    public class Meta {
        // ID（非空）
        [JsonProperty(PropertyName = "id", NullValueHandling = NullValueHandling.Ignore)]
        public string Id { set; get; }

        // 原图URL
        [JsonProperty(PropertyName = "uhd", NullValueHandling = NullValueHandling.Ignore)]
        public string Uhd { set; get; }

        // 缩略图URL
        [JsonProperty(PropertyName = "thumb", NullValueHandling = NullValueHandling.Ignore)]
        public string Thumb { set; get; }

        // 标题
        [JsonProperty(PropertyName = "title", NullValueHandling = NullValueHandling.Ignore)]
        public string Title { set; get; }

        // 副标题
        [JsonProperty(PropertyName = "caption", NullValueHandling = NullValueHandling.Ignore)]
        public string Caption { set; get; }

        // 类别
        [JsonProperty(PropertyName = "cate", NullValueHandling = NullValueHandling.Ignore)]
        public string Cate { set; get; }

        // 位置
        [JsonProperty(PropertyName = "location", NullValueHandling = NullValueHandling.Ignore)]
        public string Location { set; get; }

        // 描述/图文故事
        [JsonProperty(PropertyName = "story", NullValueHandling = NullValueHandling.Ignore)]
        public string Story { set; get; }

        // 版权所有者/作者
        [JsonProperty(PropertyName = "copyright", NullValueHandling = NullValueHandling.Ignore)]
        public string Copyright { set; get; }

        // 来源URL
        [JsonProperty(PropertyName = "src", NullValueHandling = NullValueHandling.Ignore)]
        public string Src { set; get; }

        // 收录日期（非null，默认Ticks为0）
        [JsonConverter(typeof(DateConverter))]
        [JsonProperty(PropertyName = "date", NullValueHandling = NullValueHandling.Ignore)]
        public DateTime Date { set; get; }

        // 文件格式
        [JsonProperty(PropertyName = "format", NullValueHandling = NullValueHandling.Ignore)]
        public string Format { set; get; } = ".jpg";

        // 原图尺寸（默认：0,0）
        [JsonConverter(typeof(SizeConverter))]
        [JsonProperty(PropertyName = "dimen", NullValueHandling = NullValueHandling.Ignore)]
        public Size Dimen { set; get; }

        // 原图本地缓存文件
        [JsonConverter(typeof(FileConverter))]
        [JsonProperty(PropertyName = "cacheUhd", NullValueHandling = NullValueHandling.Ignore)]
        public StorageFile CacheUhd { set; get; }

        [JsonIgnore()]
        public DownloadOperation Do { set; get; }

        // 人脸位置（默认为null未检测，空则为已检测无人脸，多个则为多个人脸）
        [JsonProperty(PropertyName = "facePos", NullValueHandling = NullValueHandling.Ignore)]
        public List<Point> FacePos { set; get; }

        [JsonProperty(PropertyName = "sortFactor", NullValueHandling = NullValueHandling.Ignore)]
        public double SortFactor { set; get; }

        public bool IsValid() {
            return !string.IsNullOrEmpty(Id) && Uhd != null;
        }

        public string GetTitleOrCaption() {
            return Title ?? Caption;
        }

        public bool ExistsFaceAndAllLeft() {
            if (FacePos == null || FacePos.Count == 0 || Dimen.IsEmpty) {
                return false;
            }
            foreach (Point point in FacePos) {
                if (point.X * 1.0 / Dimen.Width >= 0.5) {
                    return false;
                }
            }
            return true;
        }

        public override string ToString() {
            return JsonConvert.SerializeObject(this);
        }
    }

    public class DateConverter : IsoDateTimeConverter {
        public DateConverter() {
            this.DateTimeFormat = "yyyy-MM-dd";
        }
    }

    public class FileConverter : JsonConverter {
        public override bool CanConvert(Type objectType) {
            return objectType == typeof(StorageFile);
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer) {
            return null;
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer) {
            writer.WriteValue((value as StorageFile).Path);
        }
    }

    public class SizeConverter : JsonConverter {
        public override bool CanConvert(Type objectType) {
            return objectType == typeof(Size);
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer) {
            return null;
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer) {
            writer.WriteValue(String.Format("{0}x{1}", ((Size)value).Width, ((Size)value).Height));
        }
    }
}
