using RSS_Stalker.Controls;
using RSS_Stalker.Dialog;
using CoreLib.Models;
using CoreLib.Tools;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Storage;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using RSS_Stalker.Tools;
using CoreLib.Enums;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using Windows.Storage.Streams;
using Microsoft.Toolkit.Uwp.Helpers;
using Microsoft.Toolkit.Uwp.Connectivity;
using CoreLib.Models.App;

// https://go.microsoft.com/fwlink/?LinkId=234238 上介绍了“空白页”项模板

namespace RSS_Stalker.Pages
{
    /// <summary>
    /// 可用于自身或导航至 Frame 内部的空白页。
    /// </summary>
    public sealed partial class SettingPage : Page
    {
        private bool _isInit = false;
        public ObservableCollection<Channel> ToastChannels = new ObservableCollection<Channel>();
        public ObservableCollection<Channel> ReadableChannels = new ObservableCollection<Channel>();
        public static SettingPage Current;
        public SettingPage()
        {
            this.InitializeComponent();
            Current = this;
            PageInit();
        }
        public async void PageInit()
        {
            string theme = AppTools.GetRoamingSetting(AppSettings.Theme,"Light");
            bool isOneDrive = Convert.ToBoolean(AppTools.GetLocalSetting(AppSettings.IsBindingOneDrive, "False"));
            string language = AppTools.GetRoamingSetting(AppSettings.Language,"en_US");
            string oneDriveUserName = AppTools.GetLocalSetting(AppSettings.UserName, "");
            string searchEngine = AppTools.GetRoamingSetting(AppSettings.SearchEngine, "Bing");
            bool isSyncWithStart = Convert.ToBoolean(AppTools.GetLocalSetting(AppSettings.SyncWithStart, "False"));
            bool isScreenChannel = Convert.ToBoolean(AppTools.GetLocalSetting(AppSettings.IsScreenChannelCustom, "False"));
            bool isScreenPage = Convert.ToBoolean(AppTools.GetLocalSetting(AppSettings.IsScreenPageCustom, "False"));
            double speechRate = Convert.ToDouble(AppTools.GetLocalSetting(AppSettings.SpeechRate, "1.0"));
            string gender = AppTools.GetLocalSetting(AppSettings.VoiceGender, "Female");
            bool isAutoCache = Convert.ToBoolean(AppTools.GetLocalSetting(AppSettings.AutoCacheWhenOpenChannel, "False"));
            bool isFirstCache = Convert.ToBoolean(AppTools.GetLocalSetting(AppSettings.IsCacheFirst, "False"));
            if (!isOneDrive)
            {
                ForceSyncContainer.Visibility = Visibility.Collapsed;
                SyncWithStartContainer.Visibility = Visibility.Collapsed;
                OneDriveNameTextBlock.Visibility = Visibility.Collapsed;
                OneDriveLogoutButton.Visibility = Visibility.Collapsed;
                LoginOneDriveButton.Visibility = Visibility.Visible;
            }
            if (theme == "Light")
                ThemeComboBox.SelectedIndex = 0;
            else
                ThemeComboBox.SelectedIndex = 1;
            if (language == "zh_CN")
                LanguageComboBox.SelectedIndex = 0;
            else
                LanguageComboBox.SelectedIndex = 1;
            switch (searchEngine)
            {
                case "Google":
                    SearchEngineComboBox.SelectedIndex = 0;
                    break;
                case "Baidu":
                    SearchEngineComboBox.SelectedIndex = 1;
                    break;
                case "Bing":
                    SearchEngineComboBox.SelectedIndex = 2;
                    break;
                default:
                    break;
            }
            AutoCacheChannel.IsOn = isAutoCache;
            FirstCacheChannel.IsOn = isFirstCache;
            CacheSizeTextBlock.Text = await IOTools.GetCacheSize();
            VoiceGenderComboBox.SelectedIndex = gender == "Female" ? 1 : 0;
            SpeechRateSlider.Value = speechRate;
            SyncWithStartSwitch.IsOn = isSyncWithStart;
            OneDriveNameTextBlock.Text = oneDriveUserName;
            ToastChannels.Clear();
            var toastList = await IOTools.GetNeedToastChannels();
            if (toastList.Count > 0)
            {
                foreach (var item in toastList)
                {
                    ToastChannels.Add(item);
                }
            }
            else
            {
                ToastGridView.Visibility = Visibility.Collapsed;
            }
            var readableList = await IOTools.GetNeedReadableChannels();
            if (readableList.Count > 0)
            {
                foreach (var item in readableList)
                {
                    ReadableChannels.Add(item);
                }
            }
            else
            {
                ReadableGridView.Visibility = Visibility.Collapsed;
            }
            if (isScreenChannel)
                ScreenChannelComboBox.SelectedIndex = 1;
            else if (isScreenPage)
                ScreenChannelComboBox.SelectedIndex = 2;
            else
                ScreenChannelComboBox.SelectedIndex = 0;
            //TestRoamingTimeBlock.Text = AppTools.GetRoamingSetting(AppSettings.BasicUpdateTime, "1");
            _isInit = true;
        }

        private void ThemeComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!_isInit)
                return;
            var item = ThemeComboBox.SelectedItem as ComboBoxItem;
            AppTools.WriteRoamingSetting(AppSettings.Theme, item.Name);
            MainPage.Current.RequestedTheme = item.Name == "Light" ? ElementTheme.Light : ElementTheme.Dark;
            new PopupToast(AppTools.GetReswLanguage("Tip_NeedRestartApp")).ShowPopup();
        }

        private void LanguageComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!_isInit)
                return;
            var item = LanguageComboBox.SelectedItem as ComboBoxItem;
            AppTools.WriteRoamingSetting(AppSettings.Language, item.Name);
            new PopupToast(AppTools.GetReswLanguage("Tip_NeedRestartApp")).ShowPopup();
        }

        private async void OneDriveLogoutButton_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new ConfirmDialog(AppTools.GetReswLanguage("Tip_LogoutWarning"), AppTools.GetReswLanguage("Tip_OneDriveLogoutTip"));
            var result = await dialog.ShowAsync();
            if (result == ContentDialogResult.Primary)
            {
                await App.OneDrive.Logout();
                AppTools.WriteLocalSetting(AppSettings.UserName, "");
                AppTools.WriteLocalSetting(AppSettings.BasicUpdateTime, "0");
                AppTools.WriteLocalSetting(AppSettings.TodoUpdateTime, "0");
                AppTools.WriteLocalSetting(AppSettings.StarUpdateTime, "0");
                AppTools.WriteLocalSetting(AppSettings.ToastUpdateTime, "0");
                AppTools.WriteLocalSetting(AppSettings.IsBindingOneDrive, "False");
                AppTools.WriteLocalSetting(AppSettings.IsLocalAccount, "False");
                var frame = Window.Current.Content as Frame;
                frame.Navigate(typeof(OneDrivePage));
                new PopupToast(AppTools.GetReswLanguage("Tip_RebindOneDrive")).ShowPopup();
            }
        }

        private async void ImportOpmlButton_Click(object sender, RoutedEventArgs e)
        {
            ImportOpmlButton.IsEnabled = false;
            ImportOpmlButton.Content = AppTools.GetReswLanguage("Tip_Waiting");
            var file = await IOTools.OpenLocalFile(".opml");
            if (file != null)
            {
                try
                {
                    var list = await AppTools.GetRssListFromFile(file);
                    if (list != null && list.Count > 0)
                    {
                        var allList = MainPage.Current.Categories.ToList();
                        foreach (var item in list)
                        {
                            allList.Add(item);
                        }
                        await IOTools.ReplaceCategory(allList, true);
                        MainPage.Current.ReplaceList(allList);
                        new PopupToast(AppTools.GetReswLanguage("Tip_ImportSuccess")).ShowPopup();
                    }
                    else
                    {
                        new PopupToast(AppTools.GetReswLanguage("Tip_ImportError"), AppTools.GetThemeSolidColorBrush(ColorType.ErrorColor)).ShowPopup();
                    }
                }
                catch (Exception ex)
                {
                    new PopupToast(ex.Message, AppTools.GetThemeSolidColorBrush(ColorType.ErrorColor)).ShowPopup();
                }
            }
            ImportOpmlButton.IsEnabled = true;
            ImportOpmlButton.Content = AppTools.GetReswLanguage("Tip_Import");
        }

        private async void ExportOpmlButton_Click(object sender, RoutedEventArgs e)
        {
            ExportOpmlButton.IsEnabled = false;
            ExportOpmlButton.Content = AppTools.GetReswLanguage("Tip_Waiting");
            var allList = MainPage.Current.Categories.ToList();
            try
            {
                var opml = new Opml(allList);
                string content = opml.ToString();
                string fileName = AppTools.GetLocalSetting(AppSettings.UserName, "") + "_" + DateTime.Now.ToString("yyyyMMdd_HH_mm_ss") + ".opml";
                var file = await IOTools.GetSaveFile(".opml", fileName, "OPML File");
                if (file != null)
                {
                    await FileIO.WriteTextAsync(file, content);
                    new PopupToast(AppTools.GetReswLanguage("Tip_ExportSuccess")).ShowPopup();
                }
            }
            catch (Exception)
            {
                new PopupToast(AppTools.GetReswLanguage("Tip_ImportError"), AppTools.GetThemeSolidColorBrush(ColorType.ErrorColor)).ShowPopup();
            }
            ExportOpmlButton.IsEnabled = true;
            ExportOpmlButton.Content = AppTools.GetReswLanguage("Tip_Export");
        }

        private async void RemoveToastButton_Click(object sender, RoutedEventArgs e)
        {
            (sender as Button).IsEnabled = false;
            var data = (sender as Button).DataContext as Channel;
            if (data != null)
            {
                if (SettingPivot.SelectedIndex == 3)
                {
                    await IOTools.RemoveNeedToastChannel(data);
                    ToastChannels.Remove(data);
                    MainPage.Current.ToastList.RemoveAll(p => p.Id == data.Id);
                }
                else if(SettingPivot.SelectedIndex==4)
                {
                    await IOTools.RemoveNeedReadableChannel(data);
                    ReadableChannels.Remove(data);
                    MainPage.Current.ReadableList.RemoveAll(p => p.Id == data.Id);
                }
                new PopupToast(AppTools.GetReswLanguage("Tip_Removed")).ShowPopup();
            }
            (sender as Button).IsEnabled = true;
        }

        private async void ForceSyncButton_Click(object sender, RoutedEventArgs e)
        {
            if (!NetworkHelper.Instance.ConnectionInformation.IsInternetAvailable)
            {
                new PopupToast(AppTools.GetReswLanguage("Tip_FailedWithoutInternet"), AppTools.GetThemeSolidColorBrush(ColorType.ErrorColor)).ShowPopup();
                return;
            }
            var btn = sender as Button;
            btn.IsEnabled = false;
            btn.Content = AppTools.GetReswLanguage("Tip_Waiting");
            var tasks = new List<Task>();
            var cateList = new List<Category>();
            var toastList = new List<Channel>();
            var pageList = new List<CustomPage>();
            var readList = new List<string>();
            var cate = Task.Run(async () =>
            {
                cateList = await App.OneDrive.GetCategoryList();
                await IOTools.ReplaceCategory(cateList);

            });
            var todo = Task.Run(async () =>
            {
                var TodoList = await App.OneDrive.GetTodoList();
                await IOTools.ReplaceTodo(TodoList);
            });
            var star = Task.Run(async () =>
            {
                var StarList = await App.OneDrive.GetStarList();
                await IOTools.ReplaceStar(StarList);
            });
            var toast = Task.Run(async () =>
            {
                toastList = await App.OneDrive.GetToastList();
                await IOTools.ReplaceToast(toastList);
            });
            var page = Task.Run(async () =>
            {
                pageList = await App.OneDrive.GetPageList();
                await IOTools.ReplacePage(pageList);
            });
            var read = Task.Run(async () =>
            {
                readList = await App.OneDrive.GetReadList();
                await IOTools.ReplaceReadIds(readList);
            });
            tasks.Add(cate);
            tasks.Add(todo);
            tasks.Add(star);
            tasks.Add(toast);
            tasks.Add(page);
            tasks.Add(read);
            try
            {
                await Task.WhenAll(tasks.ToArray());
                string basicUpdateTime = AppTools.GetRoamingSetting(AppSettings.BasicUpdateTime, "1");
                string todoUpdateTime = AppTools.GetRoamingSetting(AppSettings.TodoUpdateTime, "1");
                string starUpdateTime = AppTools.GetRoamingSetting(AppSettings.StarUpdateTime, "1");
                string toastUpdateTime = AppTools.GetRoamingSetting(AppSettings.ToastUpdateTime, "1");
                string pageUpdateTime = AppTools.GetRoamingSetting(AppSettings.PageUpdateTime, "1");
                string readUpdateTime = AppTools.GetRoamingSetting(AppSettings.ReadUpdateTime, "1");
                AppTools.WriteLocalSetting(AppSettings.BasicUpdateTime, basicUpdateTime);
                AppTools.WriteLocalSetting(AppSettings.TodoUpdateTime, todoUpdateTime);
                AppTools.WriteLocalSetting(AppSettings.StarUpdateTime, starUpdateTime);
                AppTools.WriteLocalSetting(AppSettings.ToastUpdateTime, toastUpdateTime);
                AppTools.WriteLocalSetting(AppSettings.PageUpdateTime, pageUpdateTime);
                AppTools.WriteLocalSetting(AppSettings.ReadUpdateTime, readUpdateTime);
                AppTools.WriteLocalSetting(AppSettings.IsChannelsChangeInOffline, "False");
                AppTools.WriteLocalSetting(AppSettings.IsTodoChangeInOffline, "False");
                AppTools.WriteLocalSetting(AppSettings.IsStarChangeInOffline, "False");
                AppTools.WriteLocalSetting(AppSettings.IsToastChangeInOffline, "False");
                AppTools.WriteLocalSetting(AppSettings.IsPageChangeInOffline, "False");
                AppTools.WriteLocalSetting(AppSettings.IsReadChangeInOffline, "False");
                MainPage.Current.ReplaceList(cateList);
                MainPage.Current.ReplacePageList(pageList);
                MainPage.Current.ReadIds = readList;
                ToastChannels.Clear();
                foreach (var item in toastList)
                {
                    ToastChannels.Add(item);
                }
                btn.IsEnabled = true;
                btn.Content = AppTools.GetReswLanguage("Tip_ForceSync");
                new PopupToast(AppTools.GetReswLanguage("Tip_SyncSuccess")).ShowPopup();
            }
            catch (Exception ex)
            {
                btn.IsEnabled = true;
                btn.Content = AppTools.GetReswLanguage("Tip_ForceSync");
                new PopupToast(ex.Message,AppTools.GetThemeSolidColorBrush(ColorType.ErrorColor)).ShowPopup();
            }
        }

        private async void BaiduTranslateAccountButton_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new BaiduTranslateDialog();
            await dialog.ShowAsync();
        }

        private void SyncWithStartSwitch_Toggled(object sender, RoutedEventArgs e)
        {
            if (!_isInit)
                return;
            AppTools.WriteLocalSetting(AppSettings.SyncWithStart, SyncWithStartSwitch.IsOn.ToString());
        }

        private void ScreenChannelComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!_isInit)
                return;
            if (ScreenChannelComboBox.SelectedIndex == 0)
            {
                AppTools.WriteLocalSetting(AppSettings.IsScreenChannelCustom, "False");
                AppTools.WriteLocalSetting(AppSettings.IsScreenPageCustom, "False");
            }  
            else if (ScreenChannelComboBox.SelectedIndex == 1)
            {
                AppTools.WriteLocalSetting(AppSettings.IsScreenChannelCustom, "True");
                AppTools.WriteLocalSetting(AppSettings.IsScreenPageCustom, "False");
            }
            else if (ScreenChannelComboBox.SelectedIndex == 2)
            {
                AppTools.WriteLocalSetting(AppSettings.IsScreenChannelCustom, "False");
                AppTools.WriteLocalSetting(AppSettings.IsScreenPageCustom, "True");
            }
        }

        private void SearchEngineComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!_isInit)
                return;
            var item = SearchEngineComboBox.SelectedItem as ComboBoxItem;
            AppTools.WriteRoamingSetting(AppSettings.SearchEngine, item?.Name??"Bing");
        }

        private void SpeechRateSlider_ValueChanged(object sender, RangeBaseValueChangedEventArgs e)
        {
            if (!_isInit)
                return;
            AppTools.WriteLocalSetting(AppSettings.SpeechRate, SpeechRateSlider.Value.ToString());
        }
        /// <summary>
        /// 朗读文本
        /// </summary>
        /// <param name="text">文本</param>
        /// <returns></returns>
        async Task SpeakTextAsync(string text)
        {
            TryListenButton.IsEnabled = false;
            IRandomAccessStream stream = await AppTools.SynthesizeTextToSpeechAsync(text);
            await VoiceMediaElement.PlayStreamAsync(stream, true, () =>
            {
                TryListenButton.IsEnabled = true;
            });
        }
        private async void TryListenButton_Click(object sender, RoutedEventArgs e)
        {
            string content = AppTools.GetReswLanguage("Tip_SpeechTest");
            //string content = "This choice of voice is reflected in the SpeechSynthesizer.";
            await SpeakTextAsync(content);
        }

        private void VoiceGenderComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!_isInit)
                return;
            string gender = VoiceGenderComboBox.SelectedIndex==0?"Male":"Female";
            AppTools.WriteLocalSetting(AppSettings.VoiceGender, gender);
        }

        private async void ExportLocalListButton_Click(object sender, RoutedEventArgs e)
        {
            var file = await IOTools.GetSaveFile(".json", "RSSStalker_LocalList.json", "JSON File");
            ExportLocalListButton.IsEnabled = false;
            ExportLocalListButton.Content = AppTools.GetReswLanguage("Tip_Waiting");
            if (file != null)
            {
                await IOTools.ExportLocalList(file);
                new PopupToast(AppTools.GetReswLanguage("Tip_ExportSuccess")).ShowPopup();
            }
            ExportLocalListButton.IsEnabled = true;
            ExportLocalListButton.Content = AppTools.GetReswLanguage("Tip_Export");
        }

        private async void ImportLocalListButton_Click(object sender, RoutedEventArgs e)
        {
            ImportLocalListButton.IsEnabled = false;
            ImportLocalListButton.Content = AppTools.GetReswLanguage("Tip_Waiting");
            var file = await IOTools.OpenLocalFile(".json");
            if (file != null)
            {
                try
                {
                    await IOTools.ImportLocalList(file);
                    ToastChannels.Clear();
                    var toastList = await IOTools.GetNeedToastChannels();
                    if (toastList.Count > 0)
                    {
                        foreach (var item in toastList)
                        {
                            ToastChannels.Add(item);
                        }
                    }
                    MainPage.Current.TodoList = await IOTools.GetLocalTodoReadList();
                    MainPage.Current.StarList = await IOTools.GetLocalStarList();
                    MainPage.Current.ToastList = toastList;
                    new PopupToast(AppTools.GetReswLanguage("Tip_ImportSuccess")).ShowPopup();
                }
                catch (Exception ex)
                {
                    new PopupToast(ex.Message, AppTools.GetThemeSolidColorBrush(ColorType.ErrorColor)).ShowPopup();
                }
            }
            ImportLocalListButton.IsEnabled = true;
            ImportLocalListButton.Content = AppTools.GetReswLanguage("Tip_Import");
        }

        private async void CacheAllButton_Click(object sender, RoutedEventArgs e)
        {
            if (!NetworkHelper.Instance.ConnectionInformation.IsInternetAvailable)
            {
                new PopupToast(AppTools.GetReswLanguage("Tip_FailedWithoutInternet"), AppTools.GetThemeSolidColorBrush(ColorType.ErrorColor)).ShowPopup();
                return;
            }
            CacheAllButton.IsEnabled = false;
            CacheProgressBar.Visibility = Visibility.Visible;
            
            var list = new List<Channel>();
            foreach (var item in MainPage.Current.Categories)
            {
                foreach (var cha in item.Channels)
                {
                    list.Add(cha);
                }
            }
            var pageList = MainPage.Current.CustomPages;
            if (list.Count > 0)
            {
                try
                {
                    CacheProgressBar.Maximum = list.Count+pageList.Count;
                    int channelCount = 0;
                    int pageCount = 0;
                    var tasks = new Task[2];
                    tasks[0] = Task.Run(async () => {
                        await IOTools.AddCacheChannel(async (count) =>
                        {
                            await DispatcherHelper.ExecuteOnUIThreadAsync(() =>
                            {
                                channelCount = count;
                                CacheProgressBar.Value = channelCount + pageCount;
                            });
                        }, list.ToArray());
                    });
                    tasks[1] = Task.Run(async () => {
                        await IOTools.AddCachePage(async (count) =>
                        {
                            await DispatcherHelper.ExecuteOnUIThreadAsync(() =>
                            {
                                pageCount = count;
                                CacheProgressBar.Value = channelCount + pageCount;
                            });
                        }, pageList.ToArray());
                    });
                    await Task.WhenAll(tasks);
                    new PopupToast(AppTools.GetReswLanguage("Tip_CacheSuccess")).ShowPopup();
                }
                catch (Exception ex)
                {
                    new PopupToast(ex.Message).ShowPopup();
                }
            }
            CacheSizeTextBlock.Text = await IOTools.GetCacheSize();
            CacheAllButton.IsEnabled = true;
            CacheProgressBar.Visibility = Visibility.Collapsed;
        }

        private async void ClearCacheButton_Click(object sender, RoutedEventArgs e)
        {
            await IOTools.DeleteCache();
            new PopupToast(AppTools.GetReswLanguage("Tip_ClearSuccess")).ShowPopup();
            CacheSizeTextBlock.Text = await IOTools.GetCacheSize();
        }

        private void AutoCacheChannel_Toggled(object sender, RoutedEventArgs e)
        {
            if (!_isInit)
                return;
            AppTools.WriteLocalSetting(AppSettings.AutoCacheWhenOpenChannel, AutoCacheChannel.IsOn.ToString());
        }

        private void FirstCacheChannel_Toggled(object sender, RoutedEventArgs e)
        {
            if (!_isInit)
                return;
            AppTools.WriteLocalSetting(AppSettings.IsCacheFirst, FirstCacheChannel.IsOn.ToString());

        }

        private async void TranslateOptionButton_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new BaiduTranslateDialog();
            await dialog.ShowAsync();
        }
    }
}
