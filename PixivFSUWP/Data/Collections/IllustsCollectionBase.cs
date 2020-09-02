using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using Windows.Foundation;
using Windows.UI.Xaml.Data;

namespace PixivFSUWP.Data.Collections
{
    public abstract class IllustsCollectionBase<TViewModel> : ObservableCollection<TViewModel>, ISupportIncrementalLoading
    {
        protected string nexturl = "begin";
        protected bool _busy = false;
        protected bool _emergencyStop = false;
        protected EventWaitHandle pause = new ManualResetEvent(true);
        public double VerticalOffset;

        public virtual IAsyncOperation<LoadMoreItemsResult> LoadMoreItemsAsync(uint count)
        {
            if (_busy)
                throw new InvalidOperationException("Only one operation in flight at a time");
            _busy = true;
            return AsyncInfo.Run((c) => LoadMoreItemsAsync(c, count));
        }

        public virtual bool HasMoreItems => !string.IsNullOrEmpty(nexturl);

        public virtual void StopLoading()
        {
            Debug.WriteLine(_busy);
            _emergencyStop = true;
            if (_busy)
            {
                ResumeLoading();
            }
            else
            {
                Clear();
                GC.Collect();
            }
        }

        public virtual void PauseLoading()
        {
            Debug.WriteLine("Pause");
            pause.Reset();
        }

        public virtual void ResumeLoading()
        {
            Debug.WriteLine("Resume");
            pause.Set();
        }

        protected abstract Task<LoadMoreItemsResult> LoadMoreItemsAsync(CancellationToken c, uint count);

        protected virtual void LoadMoreItemsAsync_Finally()
        {
            Debug.WriteLine("[LoadMoreItemsAsync]\tFinally");
            _busy = false;
            if (!_emergencyStop) return;
            nexturl = string.Empty;
            Clear();
            GC.Collect();
        }
    }
}
