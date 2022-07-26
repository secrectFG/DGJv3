using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnLockMusic;

namespace DGJv3.InternalModule
{
    internal class LwMultiApi : LwlApiBaseModule
    {
        
        internal LwMultiApi()
        {
            SetServiceName("nulti");
            SetInfo("多平台", "谜之铁皮", "", "1.0", "QQ音乐 酷狗音乐 酷我音乐 网易云");
        }

        protected override SongInfo Search(string keyword)
        {
            clsMusicOperation clsMusicOperation = new clsMusicOperation();
            var msc = clsMusicOperation.GetMusicList(keyword).FirstOrDefault();
            if (msc != null)
            {
                var name = msc.Name;
                var songInfo = new SongInfo(
                    this,
                    ""+ name + msc.Singer + msc.Source,
                    name,
                    new string[] { msc.Singer }
                );
                songInfo.Tag = msc;
                return songInfo;
            }
            return null;
        }

        protected override string GetDownloadUrl(SongItem songInfo)
        {
            var msc = songInfo.Tag as clsMusic;
            return msc.DownloadURL;
        }
    }
}
