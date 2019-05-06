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
    public sealed partial class FeedDetailPage : Page
    {
        private Feed _sourceFeed;
        public FeedDetailPage()
        {
            this.InitializeComponent();
        }
        protected override async void OnNavigatedTo(NavigationEventArgs e)
        {
            if(e.Parameter!=null && e.Parameter is Feed)
            {
                _sourceFeed = e.Parameter as Feed;
                TitleTextBlock.Text = _sourceFeed.Title;
                LoadingRing.IsActive = true;
                string css = await FileIO.ReadTextAsync(await StorageFile.GetFileFromApplicationUriAsync(new Uri("ms-appx:///Template/Default.css")));
                string html = AppTools.GetHTML(css, _sourceFeed.Content ?? "");
                DetailWebView.NavigateToString(html);
            }
        }

        private void DetailWebView_Loaded(object sender, RoutedEventArgs e)
        {
            LoadingRing.IsActive = false;
        }
    }
}
