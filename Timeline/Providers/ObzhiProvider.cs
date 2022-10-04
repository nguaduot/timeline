﻿using Timeline.Beans;
using Timeline.Utils;
using Newtonsoft.Json;
using System;
using System.Diagnostics;
using System.Net.Http;
using System.Net.NetworkInformation;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Threading;

namespace Timeline.Providers {
    public class ObzhiProvider : BaseProvider {
        private const string URL_API = "https://api.nguaduot.cn/obzhi/v2" +
            "?client=timelinewallpaper&device={0}" +
            "&order={1}&cate={2}" +
            "&tag={3}&no={4}&date={5}&score={6}&admin={7}";

        private Meta ParseBean(ObzhiApiData bean) {
            Meta meta = new Meta {
                Id = bean.Id,
                No = bean.No,
                Uhd = bean.ImgUrl,
                Thumb = bean.ThumbUrl,
                Title = bean.Title,
                Story = bean.Story,
                Copyright = "@" + bean.Copyright,
                Cate = bean.CateName,
                Score = bean.Score,
                Format = FileUtil.ParseFormat(bean.ImgUrl)
            };
            //DateTime.TryParseExact(bean.RelDate, "yyyy-MM-dd", new CultureInfo("en-US"), DateTimeStyles.None, out DateTime date);
            if (DateTime.TryParse(bean.RelDate, out DateTime date)) {
                meta.Date = date;
            }
            return meta;
        }

        public override async Task<bool> LoadData(CancellationToken token, BaseIni bi, Go go) {
            int no = go.No;
            DateTime date = go.Date.Ticks > 0 ? go.Date : DateTime.Now;
            float score = go.Score;
            if ("date".Equals(bi.Order)) {
                no = Math.Min(no, GetMinNo());
                date = GetMinDate() < date ? GetMinDate() : date;
                score = Math.Min(score, GetMinScore());
            } else if ("score".Equals(bi.Order)) {
                score = Math.Min(score, GetMinScore());
            }
            string urlApi = string.Format(URL_API, SysUtil.GetDeviceId(),
                bi.Order, bi.Cate,
                go.Tag, no, date.ToString("yyyyMMdd"), score, go.Admin);
            LogUtil.D("LoadData() provider url: " + urlApi);
            try {
                HttpClient client = new HttpClient();
                HttpResponseMessage res = await client.GetAsync(urlApi, token);
                string jsonData = await res.Content.ReadAsStringAsync();
                //LogUtil.D("LoadData() provider data: " + jsonData.Trim());
                ObzhiApi api = JsonConvert.DeserializeObject<ObzhiApi>(jsonData);
                if (api.Status != 1) {
                    return false;
                }
                List<Meta> metasAdd = new List<Meta>();
                foreach (ObzhiApiData item in api.Data) {
                    metasAdd.Add(ParseBean(item));
                }
                AppendMetas(metasAdd);
                return true;
            } catch (Exception e) {
                // 情况1：任务被取消
                // System.Threading.Tasks.TaskCanceledException: A task was canceled.
                LogUtil.E("LoadData() " + e.Message);
            }
            return false;
        }
    }
}
