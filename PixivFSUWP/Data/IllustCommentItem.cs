using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Windows.Data.Json;

namespace PixivFSUWP.Data
{
    public class IllustCommentItem
    {
        public int UserID { get; set; }
        public int ID { get; set; }
        public string Comment { get; set; }
        public string DateTime { get; set; }
        public string UserName { get; set; }
        public string UserAccount { get; set; }
        public string AvatarUrl { get; set; }
        public int ParentCommentID { get; set; }

        public static IllustCommentItem FromObject(PixivCS.Objects.Comment Source)
        {
            IllustCommentItem toret = new IllustCommentItem
            {
                UserID = (int)Source.User.Id,
                ID = (int)Source.Id,
                Comment = Source.CommentComment,
                DateTime = Source.Date,
                UserName = Source.User.Name,
                UserAccount = Source.User.Account,
                AvatarUrl = Source.User.ProfileImageUrls.Medium?.ToString() ?? ""
            };
            if (Source.ParentComment.CommentComment != null)
            {
                //有父级评论
                toret.ParentCommentID = (int)Source.ParentComment.Id;
            }
            else
            {
                toret.ParentCommentID = -1;
            }
            return toret;
        }
    }
}
