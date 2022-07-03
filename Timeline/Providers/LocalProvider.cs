using Timeline.Beans;
using Timeline.Utils;
using System;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Threading;
using Windows.Storage;
using Windows.ApplicationModel;
using Windows.Storage.FileProperties;

namespace Timeline.Providers {
    public class LocalProvider : BaseProvider {        
        private async Task<Meta> ParseBean(StorageFile file, int index) {
            Meta meta = new Meta {
                Uhd = file.Path,
                Thumb = file.Path,
                Format = file.FileType,
            };
            BasicProperties properties = await file.GetBasicPropertiesAsync();
            if (file.Name.Contains(".")) {
                meta.Id = properties.Size + "-" + file.Name.Replace(file.FileType, "");
                meta.Format = file.FileType;
            } else {
                // file.FileType == "."
                meta.Id = properties.Size + "-" + file.Name;
            }
            meta.Date = properties.ItemDate.DateTime;
            meta.SortFactor = properties.ItemDate.Ticks;
            string folderName = (await file.GetParentAsync()).Name;
            meta.Title = string.Format("{0} #{1}", folderName, index); // 创建日期升序
            return meta;
        }

        public override async Task<bool> LoadData(CancellationToken token, BaseIni bi, int index, DateTime date = new DateTime()) {
            // 已加载过无需加载
            if (metas.Count > 0) {
                return true;
            }
            await base.LoadData(token, bi, index, date);

            LocalIni ini = bi as LocalIni;
            StorageFolder folder = await FileUtil.GetPicLibFolder(ini.Folder);
            LogUtil.D("LoadData() provider folder: " + folder.Path);
            IReadOnlyList<StorageFile> imgFiles = await folder.GetFilesAsync(Windows.Storage.Search.CommonFileQuery.OrderByDate);
            LogUtil.D("LoadData() provider inventory: " + imgFiles.Count);
            List<Meta> metasAdd = new List<Meta>();
            for (int i = 0; i < imgFiles.Count; ++i) {
                if (imgFiles[i].ContentType.StartsWith("image")) {
                    metasAdd.Add(await ParseBean(imgFiles[i], imgFiles.Count - i));
                }
            }
            RandomMetas(metasAdd);
            return true;
        }
    }
}
