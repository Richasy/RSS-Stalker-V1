using RSS_Stalker.Controls;
using CoreLib.Models;
using CoreLib.Tools;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.ApplicationModel.DataTransfer;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.System;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using CoreLib.Enums;
using Windows.UI.Xaml.Media.Animation;
using Rss.Parsers.Rss;

// https://go.microsoft.com/fwlink/?LinkId=234238 上介绍了“空白页”项模板

namespace RSS_Stalker.Pages
{
    /// <summary>
    /// 可用于自身或导航至 Frame 内部的空白页。
    /// </summary>
    public sealed partial class FeedCollectionPage : Page
    {
        private ObservableCollection<RssSchema> FeedCollection = new ObservableCollection<RssSchema>();
        private List<RssSchema> AllFeed = new List<RssSchema>();
        public static FeedCollectionPage Current;
        /// <summary>
        /// 用于处理待读列表和收藏列表的简易文章集合页面
        /// </summary>
        public FeedCollectionPage()
        {
            this.InitializeComponent();
            NavigationCacheMode = NavigationCacheMode.Enabled;
            Current = this;
        }
        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            MainPage.Current._isChannelAbout = false;
            if (e.NavigationMode == NavigationMode.Back)
            {
                return;
            }
            if (e.Parameter != null)
            {
                NoDataTipContainer.Visibility = Visibility.Collapsed;
                if (e.Parameter is Tuple<List<RssSchema>,string>)
                {
                    var data = e.Parameter as Tuple<List<RssSchema>, string>;
                    var feed = data.Item1;
                    AllFeed = feed;
                    TitleTextBlock.Text = data.Item2;
                    FeedCollection.Clear();
                    foreach (var item in feed)
                    {
                        FeedCollection.Add(item);
                    }
                }
            }
        }
        public void UpdateLayout(List<RssSchema> feed,string title)
        {
            LoadingRing.IsActive = true;
            NoDataTipContainer.Visibility = Visibility.Collapsed;
            TitleTextBlock.Text = title;
            FeedCollection.Clear();
            AllFeed = feed;
            if (feed != null && feed.Count > 0)
            {
                foreach (var item in feed)
                {
                    FeedCollection.Add(item);
                }
            }
            else
            {
                NoDataTipContainer.Visibility = Visibility.Visible;
            }
            LoadingRing.IsActive = false;
        }
        private void FeedGridView_ItemClick(object sender, ItemClickEventArgs e)
        {
            var item = e.ClickedItem as RssSchema;
            var t = new Tuple<RssSchema, List<RssSchema>>(item, FeedCollection.ToList());
            var text = AppTools.GetChildObject<TextBlock>(sender as FrameworkElement, "TitleBlock");
            ConnectedAnimationService.GetForCurrentView().PrepareToAnimate("ForwardConnectedAnimation", text);
            MainPage.Current.MainFrame.Navigate(typeof(FeedDetailPage), t);
        }

        private void FeedSearchBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            string text = FeedSearchBox.Text?.Trim();
            FeedCollection.Clear();
            if (string.IsNullOrEmpty(text))
            {
                foreach (var item in AllFeed)
                {
                    FeedCollection.Add(item);
                }
            }
            else
            {
                var list = AllFeed.Where(p => p.Title.IndexOf(text, StringComparison.CurrentCultureIgnoreCase) != -1);
                foreach (var item in list)
                {
                    FeedCollection.Add(item);
                }
            }
        }
    }
}
