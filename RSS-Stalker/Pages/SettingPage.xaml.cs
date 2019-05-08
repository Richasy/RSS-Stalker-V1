using RSS_Stalker.Controls;
using RSS_Stalker.Dialog;
using RSS_Stalker.Models;
using RSS_Stalker.Tools;
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

// https://go.microsoft.com/fwlink/?LinkId=234238 上介绍了“空白页”项模板

namespace RSS_Stalker.Pages
{
    /// <summary>
    /// 可用于自身或导航至 Frame 内部的空白页。
    /// </summary>
    public sealed partial class SettingPage : Page
    {
        private bool _isInit = false;
        public SettingPage()
        {
            this.InitializeComponent();
            PageInit();
        }
        public void PageInit()
        {
            string theme = AppTools.GetRoamingSetting(Enums.AppSettings.Theme, "Light");
            string language = AppTools.GetRoamingSetting(Enums.AppSettings.Language, "zh_CN");
            string oneDriveUserName = AppTools.GetLocalSetting(Enums.AppSettings.UserName, "");
            if (theme == "Light")
                ThemeComboBox.SelectedIndex = 0;
            else
                ThemeComboBox.SelectedIndex = 1;
            if (language == "zh_CN")
                LanguageComboBox.SelectedIndex = 0;
            else
                LanguageComboBox.SelectedIndex = 1;
            OneDriveNameTextBlock.Text = oneDriveUserName;
            _isInit = true;
        }

        private void ThemeComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!_isInit)
                return;
            var item = ThemeComboBox.SelectedItem as ComboBoxItem;
            AppTools.WriteRoamingSetting(Enums.AppSettings.Theme, item.Name);
            MainPage.Current.RequestedTheme = item.Name == "Light" ? ElementTheme.Light : ElementTheme.Dark;
            new PopupToast(AppTools.GetReswLanguage("Tip_NeedRestartApp")).ShowPopup();
        }

        private void LanguageComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!_isInit)
                return;
            var item = LanguageComboBox.SelectedItem as ComboBoxItem;
            AppTools.WriteRoamingSetting(Enums.AppSettings.Language, item.Name);
            new PopupToast(AppTools.GetReswLanguage("Tip_NeedRestartApp")).ShowPopup();
        }

        private async void OneDriveLogoutButton_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new ConfirmDialog(AppTools.GetReswLanguage("Tip_LogoutWarning"), AppTools.GetReswLanguage("Tip_OneDriveLogoutTip"));
            var result = await dialog.ShowAsync();
            if (result == ContentDialogResult.Primary)
            {
                await App.OneDrive.Logout();
                AppTools.WriteLocalSetting(Enums.AppSettings.UserName, "");
                AppTools.WriteLocalSetting(Enums.AppSettings.BasicUpdateTime, "0");
                AppTools.WriteLocalSetting(Enums.AppSettings.IsBindingOneDrive, "False");
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
                string fileName = AppTools.GetLocalSetting(Enums.AppSettings.UserName, "") + "_" + DateTime.Now.ToString("yyyyMMdd_HH_mm_ss") + ".opml";
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
    }
}
