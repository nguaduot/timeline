using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Timeline.Beans {
    public class Go {
        //public enum GoCmd {
        //    Index, // 从1开始的索引
        //    No, // 从1开始的序号
        //    Date, // 日期
        //    Score, // 热度分
        //    Tag, // 标签
        //    Empty // 无效值
        //}

        private int index = 1; // 从1开始的索引
        public int Index {
            set => index = Math.Max(1, value);
            get => index;
        }

        public int No { set; get; } = int.MaxValue; // 从1开始的序号

        [JsonConverter(typeof(DateConverter))]
        public DateTime Date { set; get; } = DateTime.Now; // 日期（Ticks > 0 有效）

        public int Score { set; get; } = int.MaxValue; // 热度分

        private string tag; // 标签
        public string Tag {
            set => tag = (tag ?? "").Trim();
            get => tag;
        }

        public static DateTime ParseDate(string text) {
            DateTime now = DateTime.Now;
            if (string.IsNullOrEmpty(text)) {
                return new DateTime();
            }
            if (Regex.Match(text, @"\d").Success) {
                if (text.Length == 8) {
                    text = text.Substring(0, 4) + "-" + text.Substring(4, 2) + "-" + text.Substring(6);
                } else if (text.Length == 6) {
                    text = text.Substring(0, 2) + "-" + text.Substring(2, 2) + "-" + text.Substring(4);
                } else if (text.Length == 5) {
                    text = text.Substring(0, 2) + "-" + text.Substring(2, 1) + "-" + text.Substring(3);
                } else if (text.Length == 4) {
                    text = text.Substring(0, 2) + "-" + text.Substring(2);
                } else if (text.Length == 3) {
                    text = text.Substring(0, 1) + "-" + text.Substring(1);
                } else if (text.Length == 2) {
                    if (int.Parse(text) > DateTime.DaysInMonth(now.Year, now.Month)) {
                        text = text.Substring(0, 1) + "-" + text.Substring(1);
                    } else {
                        text = now.Month + "-" + text;
                    }
                } else if (text.Length == 1) {
                    text = now.Month + "-" + text;
                }
            }
            if (DateTime.TryParse(text, out DateTime date)) {
                return date;
            }
            return new DateTime();
        }

        public static Go Parse(string text) {
            Go go = new Go();
            Match match = Regex.Match(text, "^[\"'](.*)[\"']$");
            if (match.Success) { // 指定按 Tag 识别
                go.Tag = match.Groups[1].Value;
            } else {
                MatchCollection mc = Regex.Matches(text, "([iInN#dDsS])(\\d+)");
                foreach (Match m in mc.Cast<Match>()) { // 按指定规则识别
                    switch (m.Groups[1].Value) {
                        case "i":
                        case "I":
                            go.Index = int.Parse(match.Groups[2].Value);
                            break;
                        case "n":
                        case "N":
                        case "#":
                            go.No = int.Parse(match.Groups[2].Value);
                            break;
                        case "s":
                        case "S":
                            go.Score = int.Parse(match.Groups[2].Value);
                            break;
                        default: // dD
                            go.Date = ParseDate(match.Groups[2].Value);
                            break;
                    }
                }
                if (mc.Count == 0) { // 推测
                    if (DateTime.TryParseExact(text, "yyyyMMdd", new CultureInfo("en-US"), DateTimeStyles.None, out DateTime date)) {
                        go.Date = date;
                    } else if (int.TryParse(text, out int index)) {
                        go.Index = index;
                    } else if (text.Length > 0) { // 其他归为 Tag
                        go.Tag = text;
                    }
                }
            }
            return go;
        }
    }
}
