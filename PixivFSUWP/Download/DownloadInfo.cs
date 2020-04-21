using PixivFSUWP.ViewModels;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PixivFSUWP.Download
{
    public class DownloadInfo : INotifyPropertyChanged
    {
        // 文件保存路径
        public string FilePath;
        // 文件名
        public string FileName;
        // 图片标题
        public string Title;
        // PID
        public string IllustID;
        // 远程地址
        public string OriginalUrl;
        // 图片类型
        public string IllustType;
        // 下载进度
        public ulong Progress;
        // 文件大小
        public ulong FileSize;

        public DownloadStatus DownloadStatus;

        public event PropertyChangedEventHandler PropertyChanged;

    }
    public enum DownloadStatus
    {
        Ready,Running,Finished, Failed
    }
}
