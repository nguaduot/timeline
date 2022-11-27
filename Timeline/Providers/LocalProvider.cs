using Timeline.Beans;
using Timeline.Utils;
using System;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Threading;
using Windows.Storage;
using Windows.Storage.FileProperties;
using System.Linq;

namespace Timeline.Providers {
    public class LocalProvider : BaseProvider {
        private const int PAGE_SIZE = 99;

        private async Task<Meta> ParseBean(StorageFile file) {
            Meta meta = new Meta {
                Uhd = file.Path,
                Thumb = file.Path,
                Caption = file.Name,
                Format = ".".Equals(file.FileType) ? ".jpg" : file.FileType,
            };
            BasicProperties properties = await file.GetBasicPropertiesAsync();
            meta.Id = properties.Size + "-" + meta.Caption;
            meta.Date = properties.ItemDate.DateTime;
            return meta;
        }

        private void FixTitle(List<Meta> metas, StorageFolder folder) {
            metas = metas.OrderBy(m => m.Date).ToList();
            //string folderName = (await file.GetParentAsync()).Name;
            string folderName = folder.Name.Length > 16 ? folder.Name.Substring(0, 16) + "..." : folder.Name;
            for (int i = 0; i < metas.Count; ++i) {
                metas[i].Title = string.Format("~\\{0} #{1}", folderName, i + 1); // 创建日期升序
            }
        }

        public override async Task<bool> LoadData(CancellationToken token, Ini ai, BaseIni bi, Go go) {
            LocalIni ini = bi as LocalIni;
            StorageFolder folder = await FileUtil.GetGalleryFolder(ini.Folder, ai?.Folder);
            if (folder == null) {
                return false;
            }
            LogUtil.D("LoadData() provider folder: " + folder.Path);
            try {
                // 某些文件夹使用 CommonFileQuery.OrderByDate 会异常
                //IReadOnlyList<StorageFile> imgFiles = await folder.GetFilesAsync(Windows.Storage.Search.CommonFileQuery.OrderByDate);
                IReadOnlyList<StorageFile> imgFiles = await folder.GetFilesAsync();
                if (ini.Depth > 0) { // 第一层
                    foreach (StorageFolder folder1 in await folder.GetFoldersAsync()) {
                        imgFiles = imgFiles.Concat(await folder1.GetFilesAsync()).ToArray();
                        if (ini.Depth > 1) { // 第二层
                            foreach (StorageFolder folder2 in await folder1.GetFoldersAsync()) {
                                imgFiles = imgFiles.Concat(await folder2.GetFilesAsync()).ToArray();
                            }
                        }
                    }
                }
                //imgFiles = imgFiles.OrderBy(async f => (await f.GetBasicPropertiesAsync()).ItemDate.Ticks).ToArray();
                LogUtil.D("LoadData() provider inventory: " + imgFiles.Count);
                List<string> loadedNames = new List<string>();
                foreach (Meta meta in GetMetas()) {
                    loadedNames.Add(meta.Caption);
                }
                List<Meta> metasAdd = new List<Meta>();
                foreach (StorageFile file in imgFiles) {
                    if (file.ContentType.StartsWith("image") && !loadedNames.Contains(file.Name)) {
                        metasAdd.Add(await ParseBean(file));
                    }
                    if (metasAdd.Count >= PAGE_SIZE) {
                        break;
                    }
                }
                if ("date".Equals(ini.Order)) {
                    AppendMetas(metasAdd.OrderByDescending(m => m.Date).ToList());
                } else { // random
                    AppendMetas(metasAdd.OrderBy(p => Guid.NewGuid()).ToList());
                }
                FixTitle(GetMetas(), folder); // 生成标题（使用复制集合，避免打乱原序）
                return true;
            } catch (Exception e) {
                LogUtil.E("LoadData() " + e.Message);
            }
            return false;
        }
    }
}
