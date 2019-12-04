using PixivFSUWP.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Data.Json;
using Windows.Storage;

namespace PixivFSUWP
{
    public class StaticFuncs
    {
        public static string GetPicName(IllustDetail illust)
        {
            ApplicationDataContainer lset = ApplicationData.Current.LocalSettings;
            string pformat = lset.Values["PicName"] as string;
            if (string.IsNullOrWhiteSpace(pformat))
            {
                throw new SettingNullException();
            }
            var tmp1 = illust.OriginalUrls[0].Split('.');

            string ext = tmp1[tmp1.Length - 1];
            string ret = pformat;
            {
                ret = ret.Replace("${pid}", illust.IllustID.ToString());
                ret = ret.Replace("${title}", illust.Title);
                ret = ret.Replace("${uid}", illust.AuthorID.ToString());
                ret = ret.Replace("${uname}", illust.Author);
                ret = ret.Replace("${date}", illust.CreateDate);
            }
            return ret + ext;
        }

        [Serializable]
        public class SettingNullException : Exception
        {
            public SettingNullException() : base("设置项不能为空") { }
            public SettingNullException(string message) : base(message) { }
            public SettingNullException(string message, Exception inner) : base(message, inner) { }
            protected SettingNullException(
              System.Runtime.Serialization.SerializationInfo info,
              System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
        }
    }
}
