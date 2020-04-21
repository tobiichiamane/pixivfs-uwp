using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Storage;
using Windows.Storage.Pickers;
using PixivCS;
using System.IO;
using Windows.Storage.Provider;
using Windows.UI.Xaml.Controls;
using PixivFSUWP.Data;
using PixivFSUWP.ViewModels;
using System.Collections;

namespace PixivFSUWP.Download
{
    public static class DownloadManager
    {
        private static List<DownloadInfo> Queue = new List<DownloadInfo>();
        static async Task Analyze(WaterfallItemViewModel tapped)
        {
            (string picDir, string nameTemplate) = SettingsChecker();
            Windows.Data.Json.JsonObject res;
            System.Diagnostics.Debug.WriteLine(string.Format("[DownloadQueue]发送请求:{0}", tapped.Title));
            try
            {
                res = await new PixivAppAPI(Data.OverAll.GlobalBaseAPI).IllustDetail(tapped.ItemId.ToString());
            }
            catch (System.Net.Http.HttpRequestException ex)// 发送请求时发生错误。
            {
                System.Diagnostics.Debug.WriteLine(string.Format("[DownloadQueue]请求失败:{0}", ex.Message));
                return;
            }
            var illust = Data.IllustDetail.FromJsonValue(res);
            for (ushort loopnum = 0; loopnum < illust.OriginalUrls.Count; loopnum++)
            {
                string picName = GetPicName(nameTemplate, illust, loopnum);
                if (File.Exists((picDir + "\\" + picName).Replace("\\\\", "\\")))
                {
                    // 文件存在
                }
                var info = new DownloadInfo()
                {
                    FileName = picName,
                    FilePath = picDir,
                    Title = tapped.Title,
                    OriginalUrl = illust.OriginalUrls[loopnum],
                    IllustType = illust.Type,
                    IllustID = illust.IllustID.ToString(),
                    DownloadStatus = DownloadStatus.Ready
                };
                Queue.Add(info);
            }
        }
        static (string picDir, string picName) SettingsChecker()
        {
            // 判断下载路径是否未设置
            if (!(ApplicationData.Current.LocalSettings.Values["PictureSaveDirectory"] is string picDir) ||
              string.IsNullOrWhiteSpace(picDir))
                ApplicationData.Current.LocalSettings.Values["PictureSaveDirectory"] = picDir = KnownFolders.SavedPictures.Path;
            // 判断保存文件模板名是否未设置
            if (!(ApplicationData.Current.LocalSettings.Values["PictureSaveName"] is string picName) ||
                string.IsNullOrWhiteSpace(picName))
                ApplicationData.Current.LocalSettings.Values["PictureSaveName"] = picName = "${picture_id}_${picture_page}";


            return (picDir, picName);
        }

        static string GetPicName(string s, IllustDetail illust, ushort p)
        {
            return System.Text.RegularExpressions.Regex.Replace(s
                // PID
                .Replace("$P", illust.IllustID.ToString())
                // 分P数
                .Replace("$p", p.ToString())
                // 标题
                .Replace("$t", illust.Title)
                // 来源URL
                .Replace("$l", illust.OriginalUrls[p])
                // 上传日期
                .Replace("$d", illust.CreateDate)
                // 作者UID
                .Replace("$A", illust.AuthorID.ToString())
                // 作者名
                .Replace("$a", illust.Author)
                // PID
                .Replace("${picture_id}", illust.IllustID.ToString(), StringComparison.OrdinalIgnoreCase)
                // 分P数
                .Replace("${picture_page}", p.ToString(), StringComparison.OrdinalIgnoreCase)
                // 标题
                .Replace("${picture_title}", illust.Title, StringComparison.OrdinalIgnoreCase)
                // 来源URL
                .Replace("${picture_url}", illust.OriginalUrls[p], StringComparison.OrdinalIgnoreCase)
                // 上传日期
                .Replace("${upload_date}", illust.CreateDate, StringComparison.OrdinalIgnoreCase)
                // 作者UID
                .Replace("${author_id}", illust.AuthorID.ToString(), StringComparison.OrdinalIgnoreCase)
                // 作者名
                .Replace("${author_name}", illust.Author, StringComparison.OrdinalIgnoreCase)
                ,
                 // 文件名合法化 \ / : * ? " < > | 
                 "\\\\|\\/|\\:|\\*|\\?|\\<|\\>|\\||\"", " ") + "." + ((illust.Type == "ugoira") ? "gif" : illust.OriginalUrls[p].Split('.').Last());
        }
        static async Task Download(DownloadInfo info)
        {
            var downloadFolder = await StorageFolder.GetFolderFromPathAsync(info.FilePath);
            var downloadFile = await downloadFolder.CreateFileAsync(info.FileName, CreationCollisionOption.GenerateUniqueName);
            if (downloadFile is null)
                info.DownloadStatus = DownloadStatus.Failed;

            CachedFileManager.DeferUpdates(downloadFile);


            try
            {
                using (var stream = await downloadFile.OpenAsync(FileAccessMode.ReadWrite))
                {
                    // 是GIF
                    if (info.IllustType == "ugoira")
                    {
                        System.Diagnostics.Debug.WriteLine("[DownloadQueue]是一个动图");
                        using (var ugoira = await Data.UgoiraHelper.GetUgoiraAsync(info.IllustID))
                        using (var renderer = new Lumia.Imaging.GifRenderer())
                        {
                            renderer.Duration = ugoira.Frames[0].Delay;
                            renderer.Size = new Windows.Foundation.Size(ugoira.Frames[0].Image.PixelWidth, ugoira.Frames[0].Image.PixelHeight);
                            var sources = new List<Lumia.Imaging.IImageProvider>();
                            foreach (var img in ugoira.Frames)
                                sources.Add(new Lumia.Imaging.SoftwareBitmapImageSource(img.Image));
                            renderer.Sources = sources;
                            await stream.WriteAsync(await renderer.RenderAsync());
                        }
                    }
                    // 不是Gif
                    else
                    {
                        var filestream = stream.AsStream();
                        var tmpFileName = info.OriginalUrl.Split('/').Last();
                        var cachedFile = await CacheManager.GetCachedFileAsync(tmpFileName);
                        if (cachedFile is null)
                        {
                            //没有对应的缓存文件
                            using (var resStream = await (await new PixivAppAPI(OverAll.GlobalBaseAPI).RequestCall(
                                "GET", info.OriginalUrl, new Dictionary<string, string>() { { "Referer", "https://app-api.pixiv.net/" } })
                                ).Content.ReadAsStreamAsync())
                            {
                                await resStream.CopyToAsync(filestream);
                                filestream.Position = 0;
                                var newCachedFile = await CacheManager.CreateCacheFileAsync(tmpFileName);
                                using (var fileStream = await newCachedFile.Value.File.OpenStreamForWriteAsync())
                                    await filestream.CopyToAsync(fileStream);
                                await CacheManager.FinishCachedFileAsync(newCachedFile.Value, true);
                            }
                        }
                        else //有缓存文件
                            using (var fileStream = await cachedFile.OpenStreamForReadAsync())
                                await fileStream.CopyToAsync(filestream);
                    }
                }
            }
            catch (Exception ex)
            {
                info.DownloadStatus = DownloadStatus.Failed;
            }
            var updateStatus = await CachedFileManager.CompleteUpdatesAsync(downloadFile);
            if (updateStatus == FileUpdateStatus.Complete)
            {
                info.DownloadStatus = DownloadStatus.Finished;
            }
            else
            {
                info.DownloadStatus = DownloadStatus.Failed;
            }



        }

    }
}
