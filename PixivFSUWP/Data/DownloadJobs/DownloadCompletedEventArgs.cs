using System;

namespace PixivFSUWP.Data.DownloadJobs
{
    //下载完成时的事件参数
    public class DownloadCompletedEventArgs : EventArgs
    {
        public bool HasError { get; set; }
    }
}