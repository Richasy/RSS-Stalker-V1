using RSS_Stalker.Models;
using RSS_Stalker.Tools;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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
        private ObservableCollection<Feed> ShowFeeds = new ObservableCollection<Feed>();
        private List<Feed> AllFeeds = new List<Feed>();
        public FeedDetailPage()
        {
            this.InitializeComponent();
        }
        protected override async void OnNavigatedTo(NavigationEventArgs e)
        {
            if(e.Parameter!=null && e.Parameter is Tuple<Feed,List<Feed>>)
            {
                var data = e.Parameter as Tuple<Feed, List<Feed>>;
                _sourceFeed = data.Item1;
                AllFeeds = data.Item2;
                foreach (var item in data.Item2)
                {
                    if (item.InternalID != _sourceFeed.InternalID)
                    {
                        ShowFeeds.Add(item);
                    }
                }
                TitleTextBlock.Text = _sourceFeed.Title;
                LoadingRing.IsActive = true;
                string theme = AppTools.GetLocalSetting(Enums.AppSettings.Theme, "Light");
                string css = await FileIO.ReadTextAsync(await StorageFile.GetFileFromApplicationUriAsync(new Uri($"ms-appx:///Template/{theme}.css")));
                string html = AppTools.GetHTML(css, _sourceFeed.Content ?? "");
                DetailWebView.NavigateToString(html);
            }
        }

        private void DetailWebView_Loaded(object sender, RoutedEventArgs e)
        {
            LoadingRing.IsActive = false;
        }

        private async void FeedListView_ItemClick(object sender, ItemClickEventArgs e)
        {
            var data = e.ClickedItem as Feed;
            _sourceFeed = data;
            ShowFeeds.Clear();
            foreach (var item in AllFeeds)
            {
                if (item.InternalID != data.InternalID)
                {
                    ShowFeeds.Add(item);
                }
            }
            TitleTextBlock.Text = _sourceFeed.Title;
            string theme = AppTools.GetLocalSetting(Enums.AppSettings.Theme, "Light");
            string css = await FileIO.ReadTextAsync(await StorageFile.GetFileFromApplicationUriAsync(new Uri($"ms-appx:///Template/{theme}.css")));
            string html = AppTools.GetHTML(css, _sourceFeed.Content ?? "");
            DetailWebView.NavigateToString(html);
        }

        private void GridViewButton_Click(object sender, RoutedEventArgs e)
        {
            MainPage.Current.MainFrame.Navigate(typeof(ChannelDetailPage), AllFeeds);
        }
    }
}
