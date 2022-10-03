﻿using Timeline.Beans;
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
        private async Task<Meta> ParseBean(StorageFolder folder, StorageFile file, int index) {
            Meta meta = new Meta {
                Uhd = file.Path,
                Thumb = file.Path,
                Format = file.FileType,
            };
            BasicProperties properties = await file.GetBasicPropertiesAsync();
            if (file.Name.Contains(".")) {
                meta.Caption = file.Name.Replace(file.FileType, "");
                meta.Format = file.FileType;
            } else {
                // file.FileType == "."
                meta.Caption = file.Name;
            }
            meta.Id = properties.Size + "-" + meta.Caption;
            meta.Date = properties.ItemDate.DateTime;
            //string folderName = (await file.GetParentAsync()).Name;
            string folderName = folder.Name.Length > 16 ? folder.Name.Substring(0, 16) + "..." : folder.Name;
            meta.Title = string.Format("~\\{0} #{1}", folderName, index); // 创建日期升序
            return meta;
        }

        public override async Task<bool> LoadData(CancellationToken token, BaseIni bi, Go go) {
            LocalIni ini = bi as LocalIni;
            StorageFolder folder = await FileUtil.GetGalleryFolder(ini.Folder);
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
                List<Meta> metasAdd = new List<Meta>();
                for (int i = 0; i < imgFiles.Count; ++i) {
                    if (imgFiles[i].ContentType.StartsWith("image")) {
                        metasAdd.Add(await ParseBean(folder, imgFiles[i], imgFiles.Count - i));
                    }
                }
                if ("date".Equals(ini.Order)) {
                    AppendMetas(metasAdd.OrderByDescending(m => m.Date).ToList());
                } else { // random
                    AppendMetas(metasAdd.OrderBy(p => Guid.NewGuid()).ToList());
                }
                return true;
            } catch (Exception e) {
                LogUtil.E("LoadData() " + e.Message);
            }
            return false;
        }
    }
}
