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
    public class WallhavenProvider : BaseProvider {
        // 页数据索引（从1开始）（用于按需加载）
        private int pageIndex = 0;

        private const string URL_API = "https://api.nguaduot.cn/wallhaven/v2?client=timelinewallpaper&cate={0}&order={1}&page={2}";
        
        private Meta ParseBean(WallhavenApiData bean, string order) {
            Meta meta = new Meta {
                Id = bean.Id,
                Uhd = bean.ImgUrl,
                Thumb = bean.ThumbUrl,
                Title = bean.Title,
                Story = bean.Story,
                Cate = bean.CateName,
                Src = bean.SrcUrl,
                SortFactor = "score".Equals(order) ? bean.Score : bean.No
            };
            if (!string.IsNullOrEmpty(bean.Copyright)) {
                meta.Copyright = "@" + bean.Copyright;
            }
            //DateTime.TryParseExact(bean.RelDate, "yyyy-MM-dd", new CultureInfo("en-US"), DateTimeStyles.None, out DateTime date);
            if (DateTime.TryParse(bean.RelDate, out DateTime date)) {
                meta.Date = date;
            }
            return meta;
        }

        public override async Task<bool> LoadData(CancellationToken token, BaseIni ini, DateTime date = new DateTime()) {
            // 现有数据未浏览完，无需加载更多
            if (indexFocus < metas.Count - 1) {
                return true;
            }
            // 无网络连接
            if (!NetworkInterface.GetIsNetworkAvailable()) {
                return false;
            }
            await base.LoadData(token, ini, date);

            string urlApi = string.Format(URL_API, ((WallhavenIni)ini).Cate, ((WallhavenIni)ini).Order, ++pageIndex);
            LogUtil.D("LoadData() provider url: " + urlApi);
            try {
                HttpClient client = new HttpClient();
                HttpResponseMessage res = await client.GetAsync(urlApi, token);
                string jsonData = await res.Content.ReadAsStringAsync();
                //LogUtil.D("LoadData() provider data: " + jsonData.Trim());
                WallhavenApi api = JsonConvert.DeserializeObject<WallhavenApi>(jsonData);
                List<Meta> metasAdd = new List<Meta>();
                foreach (WallhavenApiData item in api.Data) {
                    metasAdd.Add(ParseBean(item, ((WallhavenIni)ini).Order));
                }
                if ("date".Equals(((WallhavenIni)ini).Order) || "score".Equals(((WallhavenIni)ini).Order)) { // 有序排列
                    SortMetas(metasAdd);
                } else {
                    AppendMetas(metasAdd);
                }
            } catch (Exception e) {
                // 情况1：任务被取消
                // System.Threading.Tasks.TaskCanceledException: A task was canceled.
                LogUtil.E("LoadData() " + e.Message);
            }

            return metas.Count > 0;
        }
    }
}