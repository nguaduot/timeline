using Timeline.Beans;
using Timeline.Utils;
using Newtonsoft.Json;
using System;
using System.Diagnostics;
using System.Net.Http;
using System.Net.NetworkInformation;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Threading;
using Windows.Storage;
using Windows.ApplicationModel;
using System.IO;
using Windows.Storage.FileProperties;

namespace Timeline.Providers {
    public class LocalProvider : BaseProvider {        
        private async Task<Meta> ParseBean(StorageFile file, int index) {
            Meta meta = new Meta {
                Id = file.GetHashCode().ToString(),
                Uhd = file.Path,
                Thumb = file.Path,
                Format = "." + file.Name.Split(".")[1],
                SortFactor = index
            };
            string folderName = (await file.GetParentAsync()).Name;
            meta.Title = string.Format("{0} #{1}", folderName, index);
            BasicProperties properties = await file.GetBasicPropertiesAsync();
            meta.Date = properties.ItemDate.DateTime;
            meta.SortFactor = properties.ItemDate.Ticks;
            return meta;
        }

        public override async Task<bool> LoadData(CancellationToken token, BaseIni bi, DateTime date = new DateTime()) {
            // 现有数据未浏览完，无需加载更多
            if (indexFocus < metas.Count - 1) {
                return true;
            }
            await base.LoadData(token, bi, date);

            StorageFolder folder = await KnownFolders.PicturesLibrary.CreateFolderAsync(AppInfo.Current.DisplayInfo.DisplayName,
                CreationCollisionOption.OpenIfExists);
            IReadOnlyList<StorageFile> imgFiles = await folder.GetFilesAsync(Windows.Storage.Search.CommonFileQuery.OrderByDate);
            List<Meta> metasAdd = new List<Meta>();
            for (int i = 0; i < imgFiles.Count; ++i) {
                Debug.WriteLine(imgFiles[i].ContentType);
                if (imgFiles[i].ContentType.StartsWith("image")) {
                    metasAdd.Add(await ParseBean(imgFiles[i], imgFiles.Count - i));
                }
            }
            if ("random".Equals(bi.Order)) { // 随机排列
                RandomMetas(metasAdd);
            } else {
                SortMetas(metasAdd);
            }
            return true;
        }
    }
}
