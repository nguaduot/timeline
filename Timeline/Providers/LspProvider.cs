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
    public class LspProvider : BaseProvider {
        private const string URL_API = "https://api.nguaduot.cn/lsp/v2?client=timelinewallpaper" +
            "&order={0}&cate={1}" +
            "&tag={2}&no={3}&date={6}&score={5}" +
            "&r22={6}&unaudited={7}&marked={8}";

        private Meta ParseBean(LspApiData bean, string order) {
            Meta meta = new Meta {
                Id = bean.Id,
                No = bean.No,
                Uhd = bean.ImgUrl,
                Thumb = bean.ThumbUrl,
                Title = bean.Album,
                Caption = bean.Title,
                Cate = bean.CateName,
                Format = FileUtil.ParseFormat(bean.ImgUrl),
                SortFactor = "score".Equals(order) ? bean.Score : bean.No
            };
            if (bean.R22 != 0) {
                if (!string.IsNullOrEmpty(meta.Title)) {
                    meta.Title = "🚫 " + meta.Title;
                } else if (!string.IsNullOrEmpty(meta.Caption)) {
                    meta.Caption = "🚫 " + meta.Caption;
                }
            }
            if (!string.IsNullOrEmpty(bean.Character)) {
                meta.Story = "@" + bean.Character;
            }
            if (!string.IsNullOrEmpty(bean.Copyright)) {
                meta.Copyright = "© " + bean.Copyright;
            }
            //DateTime.TryParseExact(bean.RelDate, "yyyy-MM-dd", new CultureInfo("en-US"), DateTimeStyles.None, out DateTime date);
            if (DateTime.TryParse(bean.RelDate, out DateTime date)) {
                meta.Date = date;
            }
            return meta;
        }

        public override async Task<bool> LoadData(CancellationToken token, BaseIni bi, Go go) {
            await base.LoadData(token, bi, go);

            LspIni ini = bi as LspIni;
            int no = GetMinNo();
            no = go.No < no ? go.No : no;
            DateTime date = GetMinDate();
            date = go.Date < date ? go.Date : date;
            float score = GetMinScore();
            score = go.Score < score ? go.Score : score;
            string urlApi = string.Format(URL_API, bi.Order, bi.Cate,
                go.Tag, no, date, score,
                ini.R22 ? SysUtil.GetDeviceId() : "",
                "unaudited".Equals(ini.Admin) ? SysUtil.GetDeviceId() : "",
                "marked".Equals(ini.Admin) ? SysUtil.GetDeviceId() : "");
            LogUtil.D("LoadData() provider url: " + urlApi);
            try {
                HttpClient client = new HttpClient();
                HttpResponseMessage res = await client.GetAsync(urlApi, token);
                string jsonData = await res.Content.ReadAsStringAsync();
                //LogUtil.D("LoadData() provider data: " + jsonData.Trim());
                LspApi api = JsonConvert.DeserializeObject<LspApi>(jsonData);
                if (api.Status != 1) {
                    return false;
                }
                List<Meta> metasAdd = new List<Meta>();
                foreach (LspApiData item in api.Data) {
                    metasAdd.Add(ParseBean(item, ini.Order));
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
