﻿using DGJv3.InternalModule;
using MaterialDesignThemes.Wpf;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;

namespace DGJv3
{
    /// <summary>
    /// DGJWindow.xaml 的交互逻辑
    /// </summary>
    internal partial class DGJWindow : Window
    {
        public DGJMain PluginMain { get; set; }

        public ObservableCollection<SongItem> Songs { get; set; }

        public ObservableCollection<SongInfo> Playlist { get; set; }

        public ObservableCollection<BlackListItem> Blacklist { get; set; }

        public Player Player { get; set; }

        public Downloader Downloader { get; set; }

        public Writer Writer { get; set; }

        public SearchModules SearchModules { get; set; }

        public DanmuHandler DanmuHandler { get; set; }

        public UniversalCommand RemoveSongCommmand { get; set; }

        public UniversalCommand RemoveAndBlacklistSongCommand { get; set; }

        public UniversalCommand RemovePlaylistInfoCommmand { get; set; }

        public UniversalCommand ClearPlaylistCommand { get; set; }

        public UniversalCommand RemoveBlacklistInfoCommmand { get; set; }

        public UniversalCommand ClearBlacklistCommand { get; set; }

        public bool IsLogRedirectDanmaku { get; set; }

        public int LogDanmakuLengthLimit { get; set; }

        public void Log(string text)
        {
            Trace.WriteLine(text);
            PluginMain.Log(text);

            if (IsLogRedirectDanmaku)
            {
                Task.Run(() =>
                {
                    try
                    {
                        if (!PluginMain.RoomId.HasValue) { return; }

                        string finalText = text.Substring(0, Math.Min(text.Length, LogDanmakuLengthLimit));
                        string result = LoginCenterAPIWarpper.Send(PluginMain.RoomId.Value, finalText);
                        if (result == null)
                        {
                            PluginMain.Log("发送弹幕时网络错误");
                        }
                        else
                        {
                            var j = JObject.Parse(result);
                            if (j["msg"].ToString() != string.Empty)
                            {
                                PluginMain.Log("发送弹幕时服务器返回：" + j["msg"].ToString());
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        if (ex.GetType().FullName.Equals("LoginCenter.API.PluginNotAuthorizedException"))
                        {
                            IsLogRedirectDanmaku = false;
                        }
                        else
                        {
                            PluginMain.Log("弹幕发送错误 " + ex.ToString());
                        }
                    }
                });
            }
        }

        public DGJWindow(DGJMain dGJMain)
        {
            void addResource(string uri)
            {
                Application.Current.Resources.MergedDictionaries.Add(new ResourceDictionary()
                {
                    Source = new Uri(uri)
                });
            }
            addResource("pack://application:,,,/MaterialDesignThemes.Wpf;component/Themes/MaterialDesignTheme.Light.xaml");
            addResource("pack://application:,,,/MaterialDesignColors;component/Themes/Recommended/Primary/MaterialDesignColor.Blue.xaml");
            addResource("pack://application:,,,/MaterialDesignColors;component/Themes/Recommended/Accent/MaterialDesignColor.DeepOrange.xaml");
            addResource("pack://application:,,,/MaterialDesignThemes.Wpf;component/Themes/MaterialDesignTheme.ProgressBar.xaml");

            DataContext = this;
            PluginMain = dGJMain;
            Songs = new ObservableCollection<SongItem>();
            Playlist = new ObservableCollection<SongInfo>();
            Blacklist = new ObservableCollection<BlackListItem>();

            Player = new Player(Songs, Playlist);
            Downloader = new Downloader(Songs);
            SearchModules = new SearchModules();
            DanmuHandler = new DanmuHandler(Songs, Player, Downloader, SearchModules, Blacklist);
            Writer = new Writer(Songs, Playlist, Player, DanmuHandler);

            Player.LogEvent += (sender, e) => {
                Log("播放:" + e.Message + (e.Exception == null ? string.Empty : e.Exception.Message)); };
            Downloader.LogEvent += (sender, e) => {
                Log("下载:" + e.Message + (e.Exception == null ? string.Empty : e.Exception.Message)); };
            Writer.LogEvent += (sender, e) => {
                Log("文本:" + e.Message + (e.Exception == null ? string.Empty : e.Exception.Message)); };
            SearchModules.LogEvent += (sender, e) => {
                Log("搜索:" + e.Message + (e.Exception == null ? string.Empty : e.Exception.Message)); };
            DanmuHandler.LogEvent += (sender, e) => {
                Log("" + e.Message + (e.Exception == null ? string.Empty : e.Exception.Message)); };

            RemoveSongCommmand = new UniversalCommand((songobj) =>
            {
                if (songobj != null && songobj is SongItem songItem)
                {
                    songItem.Remove(Songs, Downloader, Player);
                }
            });

            RemoveAndBlacklistSongCommand = new UniversalCommand((songobj) =>
            {
                if (songobj != null && songobj is SongItem songItem)
                {
                    songItem.Remove(Songs, Downloader, Player);
                    Blacklist.Add(new BlackListItem(BlackListType.Id, songItem.SongId));
                }
            });

            RemovePlaylistInfoCommmand = new UniversalCommand((songobj) =>
            {
                if (songobj != null && songobj is SongInfo songInfo)
                {
                    Playlist.Remove(songInfo);
                }
            });

            ClearPlaylistCommand = new UniversalCommand((e) =>
            {
                Playlist.Clear();
            });

            RemoveBlacklistInfoCommmand = new UniversalCommand((blackobj) =>
            {
                if (blackobj != null && blackobj is BlackListItem blackListItem)
                {
                    Blacklist.Remove(blackListItem);
                }
            });

            ClearBlacklistCommand = new UniversalCommand((x) =>
            {
                Blacklist.Clear();
            });

            InitializeComponent();

            ApplyConfig(Config.Load());

            PluginMain.ReceivedDanmaku += (sender, e) => { DanmuHandler.ProcessDanmu(e.Danmaku); };
            PluginMain.Connected += (sender, e) => { LwlApiBaseModule.RoomId = e.roomid; };
            PluginMain.Disconnected += (sender, e) => { LwlApiBaseModule.RoomId = 0; };
        }

        /// <summary>
        /// 应用设置
        /// </summary>
        /// <param name="config"></param>
        private void ApplyConfig(Config config)
        {
            Player.PlayerType = config.PlayerType;
            Player.DirectSoundDevice = config.DirectSoundDevice;
            Player.WaveoutEventDevice = config.WaveoutEventDevice;
            Player.Volume = config.Volume;
            Player.IsUserPrior = config.IsUserPrior;
            Player.IsPlaylistEnabled = config.IsPlaylistEnabled;
            SearchModules.PrimaryModule = SearchModules.Modules.FirstOrDefault(x => x.UniqueId == config.PrimaryModuleId) ?? SearchModules.NullModule;
            SearchModules.SecondaryModule = SearchModules.Modules.FirstOrDefault(x => x.UniqueId == config.SecondaryModuleId) ?? SearchModules.NullModule;
            DanmuHandler.MaxTotalSongNum = config.MaxTotalSongNum;
            DanmuHandler.MaxPersonSongNum = config.MaxPersonSongNum;
            Writer.ScribanTemplate = config.ScribanTemplate;
            IsLogRedirectDanmaku = config.IsLogRedirectDanmaku;
            LogDanmakuLengthLimit = config.LogDanmakuLengthLimit;

            LogRedirectToggleButton.IsEnabled = LoginCenterAPIWarpper.CheckLoginCenter();
            if (LogRedirectToggleButton.IsEnabled && IsLogRedirectDanmaku)
            {
                Task.Run(async () =>
                {
                    await Task.Delay(2000); // 其实不应该这么写的，不太合理
                    IsLogRedirectDanmaku = await LoginCenterAPIWarpper.DoAuth(PluginMain);
                });
            }
            else
            {
                IsLogRedirectDanmaku = false;
            }

            Playlist.Clear();
            foreach (var item in config.Playlist)
            {
                item.Module = SearchModules.Modules.FirstOrDefault(x => x.UniqueId == item.ModuleId);
                if (item.Module != null)
                {
                    Playlist.Add(item);
                }
            }

            Blacklist.Clear();
            foreach (var item in config.Blacklist)
            {
                Blacklist.Add(item);
            }
        }

        /// <summary>
        /// 收集设置
        /// </summary>
        /// <returns></returns>
        private Config GatherConfig() => new Config()
        {
            PlayerType = Player.PlayerType,
            DirectSoundDevice = Player.DirectSoundDevice,
            WaveoutEventDevice = Player.WaveoutEventDevice,
            IsUserPrior = Player.IsUserPrior,
            Volume = Player.Volume,
            IsPlaylistEnabled = Player.IsPlaylistEnabled,
            PrimaryModuleId = SearchModules.PrimaryModule.UniqueId,
            SecondaryModuleId = SearchModules.SecondaryModule.UniqueId,
            MaxPersonSongNum = DanmuHandler.MaxPersonSongNum,
            MaxTotalSongNum = DanmuHandler.MaxTotalSongNum,
            ScribanTemplate = Writer.ScribanTemplate,
            Playlist = Playlist.ToArray(),
            Blacklist = Blacklist.ToArray(),
            IsLogRedirectDanmaku = IsLogRedirectDanmaku,
            LogDanmakuLengthLimit = LogDanmakuLengthLimit,
        };

        /// <summary>
        /// 弹幕姬退出事件
        /// </summary>
        internal void DeInit()
        {
            Config.Write(GatherConfig());

            Downloader.CancelDownload();
            Player.Next();
            try
            {
                Directory.Delete(Utilities.SongsCacheDirectoryPath, true);
            }
            catch (Exception)
            {
            }
        }

        /// <summary>
        /// 主界面右侧
        /// 添加歌曲的
        /// dialog 的
        /// 关闭事件
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="eventArgs"></param>
        private void DialogAddSongs(object sender, DialogClosingEventArgs eventArgs)
        {
            if (eventArgs.Parameter.Equals(true) && !string.IsNullOrWhiteSpace(AddSongsTextBox.Text))
            {
                var keyword = AddSongsTextBox.Text;
                SongInfo songInfo = null;

                if (SearchModules.PrimaryModule != SearchModules.NullModule)
                {
                    songInfo = SearchModules.PrimaryModule.SafeSearch(keyword);
                }

                if (songInfo == null)
                {
                    if (SearchModules.SecondaryModule != SearchModules.NullModule)
                    {
                        songInfo = SearchModules.SecondaryModule.SafeSearch(keyword);
                    }
                }

                if (songInfo == null)
                {
                    Log($"点歌失败 keyword：{keyword}");
                    return;
                }

                Songs.Add(new SongItem(songInfo, "主播")); // TODO: 点歌人名字
            }
            AddSongsTextBox.Text = string.Empty;
        }

        /// <summary>
        /// 主界面右侧
        /// 添加空闲歌曲按钮的
        /// dialog 的
        /// 关闭事件
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="eventArgs"></param>
        private void DialogAddSongsToPlaylist(object sender, DialogClosingEventArgs eventArgs)
        {
            if (eventArgs.Parameter.Equals(true) && !string.IsNullOrWhiteSpace(AddSongPlaylistTextBox.Text))
            {
                var keyword = AddSongPlaylistTextBox.Text;
                SongInfo songInfo = null;

                if (SearchModules.PrimaryModule != SearchModules.NullModule)
                {
                    songInfo = SearchModules.PrimaryModule.SafeSearch(keyword);
                }

                if (songInfo == null)
                {
                    if (SearchModules.SecondaryModule != SearchModules.NullModule)
                    {
                        songInfo = SearchModules.SecondaryModule.SafeSearch(keyword);
                    }
                }

                if (songInfo == null)
                {
                    return;
                }

                Playlist.Add(songInfo);
            }
            AddSongPlaylistTextBox.Text = string.Empty;
        }

        /// <summary>
        /// 主界面右侧
        /// 添加空闲歌单按钮的
        /// dialog 的
        /// 关闭事件
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="eventArgs"></param>
        private void DialogAddPlaylist(object sender, DialogClosingEventArgs eventArgs)
        {
            if (eventArgs.Parameter.Equals(true) && !string.IsNullOrWhiteSpace(AddPlaylistTextBox.Text))
            {
                var keyword = AddPlaylistTextBox.Text;
                List<SongInfo> songInfoList = null;

                if (SearchModules.PrimaryModule != SearchModules.NullModule && SearchModules.PrimaryModule.IsPlaylistSupported)
                {
                    songInfoList = SearchModules.PrimaryModule.SafeGetPlaylist(keyword);
                }

                // 歌单只使用主搜索模块搜索

                if (songInfoList == null)
                {
                    return;
                }

                foreach (var item in songInfoList)
                {
                    Playlist.Add(item);
                }
            }
            AddPlaylistTextBox.Text = string.Empty;
        }

        /// <summary>
        /// 黑名单 popupbox 里的
        /// 添加黑名单按钮的
        /// dialog 的
        /// 关闭事件
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="eventArgs"></param>
        private void DialogAddBlacklist(object sender, DialogClosingEventArgs eventArgs)
        {
            if (eventArgs.Parameter.Equals(true)
                && !string.IsNullOrWhiteSpace(AddBlacklistTextBox.Text)
                && AddBlacklistComboBox.SelectedValue != null
                && AddBlacklistComboBox.SelectedValue is BlackListType)
            {
                var keyword = AddBlacklistTextBox.Text;
                var type = (BlackListType)AddBlacklistComboBox.SelectedValue;

                Blacklist.Add(new BlackListItem(type, keyword));
            }
            AddBlacklistTextBox.Text = string.Empty;
        }

        private void Window_Closing(object sender, CancelEventArgs e)
        {
            e.Cancel = true;
            Hide();
        }

        private async void LogRedirectToggleButton_OnChecked(object sender, RoutedEventArgs e)
        {
            try
            {
                if (!await LoginCenterAPIWarpper.DoAuth(PluginMain))
                {
                    LogRedirectToggleButton.IsChecked = false;
                }
            }
            catch (Exception)
            {
                LogRedirectToggleButton.IsChecked = false;
            }
        }
    }
}
