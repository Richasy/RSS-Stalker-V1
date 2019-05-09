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
        public static SettingPage Current;
        public SettingPage()
        {
            this.InitializeComponent();
            Current = this;
            PageInit();
        }
        public void PageInit()
        {
            string theme = AppTools.GetRoamingSetting(AppSettings.Theme, "Light");
            string language = AppTools.GetRoamingSetting(AppSettings.Language, "zh_CN");
            string oneDriveUserName = AppTools.GetLocalSetting(AppSettings.UserName, "");
            if (theme == "Light")
                ThemeComboBox.SelectedIndex = 0;
            else
                ThemeComboBox.SelectedIndex = 1;
            if (language == "zh_CN")
                LanguageComboBox.SelectedIndex = 0;
            else
                LanguageComboBox.SelectedIndex = 1;
            OneDriveNameTextBlock.Text = oneDriveUserName;
            ToastChannels.Clear();
            if (MainPage.Current.ToastList.Count > 0)
            {
                foreach (var item in MainPage.Current.ToastList)
                {
                    ToastChannels.Add(item);
                }
            }
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
                AppTools.WriteLocalSetting(AppSettings.IsBindingOneDrive, "False");
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
                        new PopupToast(AppTools.GetReswLanguage("Tip_ImportError")).ShowPopup();
                    }
                }
                catch (Exception ex)
                {
                    new PopupToast(ex.Message).ShowPopup();
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
                string fileName = AppTools.GetLocalSetting(CoreLib.Enums.AppSettings.UserName, "") + "_" + DateTime.Now.ToString("yyyyMMdd_HH_mm_ss") + ".opml";
                var file = await IOTools.GetSaveFile(".opml", fileName, "OPML File");
                if (file != null)
                {
                    await FileIO.WriteTextAsync(file, content);
                    new PopupToast(AppTools.GetReswLanguage("Tip_ExportSuccess")).ShowPopup();
                }
            }
            catch (Exception)
            {
                new PopupToast(AppTools.GetReswLanguage("Tip_ImportError")).ShowPopup();
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
                await IOTools.RemoveNeedToastChannel(data);
                ToastChannels.Remove(data);
                MainPage.Current.ToastList.RemoveAll(p => p.Id == data.Id);
                new PopupToast(AppTools.GetReswLanguage("Tip_Removed")).ShowPopup();
                return;
            }
            (sender as Button).IsEnabled = true;
        }

        private async void ForceSyncButton_Click(object sender, RoutedEventArgs e)
        {
            var btn = sender as Button;
            btn.IsEnabled = false;
            btn.Content = AppTools.GetReswLanguage("Tip_Waiting");
            var tasks = new List<Task>();
            var cateList = new List<Category>();
            var toastList = new List<Channel>();
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
            
            tasks.Add(cate);
            tasks.Add(todo);
            tasks.Add(star);
            tasks.Add(toast);
            await Task.WhenAll(tasks.ToArray());
            string basicUpdateTime = AppTools.GetRoamingSetting(AppSettings.BasicUpdateTime, "1");
            string todoUpdateTime = AppTools.GetRoamingSetting(AppSettings.TodoUpdateTime, "1");
            string starUpdateTime = AppTools.GetRoamingSetting(AppSettings.StarUpdateTime, "1");
            string toastUpdateTime = AppTools.GetRoamingSetting(AppSettings.ToastUpdateTime, "1");
            AppTools.WriteLocalSetting(AppSettings.BasicUpdateTime, basicUpdateTime);
            AppTools.WriteLocalSetting(AppSettings.TodoUpdateTime, todoUpdateTime);
            AppTools.WriteLocalSetting(AppSettings.StarUpdateTime, starUpdateTime);
            AppTools.WriteLocalSetting(AppSettings.ToastUpdateTime, toastUpdateTime);
            MainPage.Current.ReplaceList(cateList);
            ToastChannels.Clear();
            foreach (var item in toastList)
            {
                ToastChannels.Add(item);
            }
            btn.IsEnabled = true;
            btn.Content = AppTools.GetReswLanguage("Tip_ForceSync");
            new PopupToast(AppTools.GetReswLanguage("Tip_SyncSuccess")).ShowPopup();
        }
    }
}
