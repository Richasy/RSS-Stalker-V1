using CoreLib.Tools;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.System;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Imaging;
using Windows.UI.Xaml.Navigation;

// https://go.microsoft.com/fwlink/?LinkId=234238 上介绍了“空白页”项模板

namespace RSS_Stalker.Pages
{
    /// <summary>
    /// 可用于自身或导航至 Frame 内部的空白页。
    /// </summary>
    public sealed partial class WelcomePage : Page
    {
        public static WelcomePage Current;
        /// <summary>
        /// 欢迎页
        /// </summary>
        public WelcomePage()
        {
            this.InitializeComponent();
            Current = this;
            string theme = AppTools.GetRoamingSetting(CoreLib.Enums.AppSettings.Theme, "Light");
            var image = new BitmapImage(new Uri($"ms-appx:///Assets/{theme}.png"));
            AppIcon.Source = image;
            string name = AppTools.GetLocalSetting(CoreLib.Enums.AppSettings.UserName, "");
            if (string.IsNullOrEmpty(name))
                WelcomeTextBlock.Text = AppTools.GetReswLanguage("Tip_WelcomeText");
            else
                WelcomeTextBlock.Text = AppTools.GetReswLanguage("Tip_PreWelcome") + name;
        }

        private async void TutorialButton_Click(object sender, RoutedEventArgs e)
        {
            await Launcher.LaunchUriAsync(new Uri("https://www.richasy.cn/document/rss/use.html"));
        }
    }
}
