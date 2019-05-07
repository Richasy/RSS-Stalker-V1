using RSS_Stalker.Controls;
using RSS_Stalker.Tools;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
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
            if (theme == "Light")
                ThemeComboBox.SelectedIndex = 0;
            else
                ThemeComboBox.SelectedIndex = 1;
            if (language == "zh_CN")
                LanguageComboBox.SelectedIndex = 0;
            else
                LanguageComboBox.SelectedIndex = 1;

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
    }
}
