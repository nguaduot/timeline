using Newtonsoft.Json;
using System;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;

namespace Timeline.Beans {
    public class Go {
        // 索引（从1开始），大于0则忽略其他参数（互斥）
        public int Index { set; get; } = 0;

        // 最大序号（从1开始）
        public int No { set; get; } = int.MaxValue; // 从1开始的序号

        // 最大日期（Ticks > 0 有效）
        [JsonConverter(typeof(DateConverter))]
        public DateTime Date { set; get; } = DateTime.Now;

        // 最大热度分
        public int Score { set; get; } = int.MaxValue;

        // 关键词
        private string tag = "";
        public string Tag {
            set => tag = (value ?? "").Trim();
            get => tag;
        }

        // 管理员参数
        private string admin = "";
        public string Admin {
            set => admin = (value ?? "").Trim();
            get => admin;
        }

        // 源文本
        private string source;
        public string Source {
            set => source = (value ?? "").Trim();
            get => source;
        }

        public Go(string source) {
            Source = source;
        }

        public override string ToString() {
            return JsonConvert.SerializeObject(this);
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
            Go go = new Go(text);
            Match match = Regex.Match(text, "^[\"'](.*)[\"']$");
            if (match.Success) { // 指定按 Tag 识别
                go.Tag = match.Groups[1].Value;
            } else {
                MatchCollection mc1 = Regex.Matches(text, "([iInN#dDsStTaA])[\"']([^\"']+)[\"']");
                foreach (Match m in mc1.Cast<Match>()) { // 按指定规则识别
                    switch (m.Groups[1].Value) {
                        case "i":
                        case "I":
                            go.Index = int.Parse(m.Groups[2].Value);
                            break;
                        case "n":
                        case "N":
                        case "#":
                            go.No = int.Parse(m.Groups[2].Value);
                            break;
                        case "d":
                        case "D":
                            go.Date = ParseDate(m.Groups[2].Value);
                            break;
                        case "s":
                        case "S":
                            go.Score = int.Parse(m.Groups[2].Value);
                            break;
                        case "t":
                        case "T":
                            go.Tag = m.Groups[2].Value;
                            break;
                        case "a":
                        case "A":
                            go.Admin = m.Groups[2].Value;
                            break;
                    }
                }
                MatchCollection mc2 = Regex.Matches(text, "([iInN#dDsS])(\\d+)");
                foreach (Match m in mc2.Cast<Match>()) { // 按指定规则识别
                    switch (m.Groups[1].Value) {
                        case "i":
                        case "I":
                            go.Index = int.Parse(m.Groups[2].Value);
                            break;
                        case "n":
                        case "N":
                        case "#":
                            go.No = int.Parse(m.Groups[2].Value);
                            break;
                        case "d":
                        case "D":
                            go.Date = ParseDate(m.Groups[2].Value);
                            break;
                        case "s":
                        case "S":
                            go.Score = int.Parse(m.Groups[2].Value);
                            break;
                    }
                }
                if (mc1.Count + mc2.Count == 0) { // 未识别到标准规则，进行推测
                    if (DateTime.TryParseExact(text, "yyyyMMdd", new CultureInfo("en-US"), DateTimeStyles.None, out DateTime date)) {
                        go.Date = date;
                    } else if (int.TryParse(text, out int index)) {
                        go.Index = index;
                    } else if (text.Length > 0) { // 其他归为 Tag
                        go.Tag = text;
                    }
                }
            }
            if (go.Index > 0) { // Index 与其他参数互斥
                return new Go(text) { Index = go.Index };
            }
            return go;
        }

        public static string Generate(int count, int index, int no, DateTime date, float score) {
            string text = "i" + index; // 从1开始
            text += "..." + count;
            if (no > 0) {
                text += " n" + no; // 从1开始
            }
            if (date.Ticks > 0) {
                if (date.Year == DateTime.Now.Year) {
                    text += " d" + date.ToString("MMdd");
                } else {
                    text += " d" + date.ToString("yyyyMMdd");
                }
            }
            if (score > 0) {
                text += " s" + (int)Math.Ceiling(score);
            }
            return text;
        }
    }
}
