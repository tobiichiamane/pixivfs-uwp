using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Windows.UI.Xaml.Media.Imaging;

namespace PixivFSUWP.ViewModels
{
    public class CommentViewModel : INotifyPropertyChanged
    {
        public int UserID { get; set; }
        public int ID { get; set; }
        public string Comment { get; set; }
        public string UserName { get; set; }
        public string UserAccount { get; set; }
        private string _dateTime { get; set; }
        public string AvatarUrl { get; set; }
        public BitmapImage Avatar { get; set; }
        public int ParentID { get; set; }
        public ObservableCollection<CommentViewModel> ChildrenComments { get; set; } = null;

        public string DateTime
        {
            get => DateTimeOffset.Parse(_dateTime).LocalDateTime.ToString();
        }

        public event PropertyChangedEventHandler PropertyChanged;

        public async Task LoadAvatarAsync()
        {
            Avatar = await Data.OverAll.LoadImageAsync(AvatarUrl);
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("Avatar"));
        }

        public static CommentViewModel FromItem(Data.IllustCommentItem Item)
        {
            return new CommentViewModel()
            {
                ID = Item.ID,
                UserID = Item.UserID,
                Comment = Item.Comment,
                UserName = Item.UserName,
                UserAccount = "@" + Item.UserAccount,
                _dateTime = Item.DateTime,
                AvatarUrl = Item.AvatarUrl,
                ParentID = Item.ParentCommentID
            };
        }
    }
}
