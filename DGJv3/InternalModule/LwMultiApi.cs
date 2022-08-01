using System;
using System.Collections.Generic;
using System.IO;
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
            IsHandleDownload = true;
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

        public override string GetDownloadFilePath(SongItem currentSong)
        {
            var msc = currentSong.Tag as clsMusic;
            return Path.Combine(Utilities.SongsCacheDirectoryPath, CleanFileName($"{currentSong.ModuleName}-{currentSong.SongName}-{currentSong.SongId}-{msc.Source}.mp3"));
        }

        protected override DownloadStatus Download(SongItem item)
        {
            try
            {
                item.CacheForever = true;

                var path = GetDownloadFilePath(item);
                Log($"下载保存位置:{path}");
                if (File.Exists(path))
                {
                    return DownloadStatus.Success;
                }
                var dlurl = GetDownloadUrl(item);
                Log($"下载地址:{dlurl}");

                clsHttpDownloadFile.Download(dlurl, path);
            }
            catch(Exception e)
            {
                Log($"{item.SongName} 下注失败:{e}");
            }
            return DownloadStatus.Success;
        }
    }
}
