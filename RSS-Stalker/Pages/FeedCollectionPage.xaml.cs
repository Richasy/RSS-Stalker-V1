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

// https://go.microsoft.com/fwlink/?LinkId=234238 上介绍了“空白页”项模板

namespace RSS_Stalker.Pages
{
    /// <summary>
    /// 可用于自身或导航至 Frame 内部的空白页。
    /// </summary>
    public sealed partial class FeedCollectionPage : Page
    {
        private ObservableCollection<Feed> FeedCollection = new ObservableCollection<Feed>();
        private Feed _shareData = null;
        private List<Feed> AllFeed = new List<Feed>();
        public static FeedCollectionPage Current;
        /// <summary>
        /// 用于处理待读列表和收藏列表的简易文章集合页面
        /// </summary>
        public FeedCollectionPage()
        {
            this.InitializeComponent();
            Current = this;
        }
        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            if (e.Parameter != null)
            {
                if (e.Parameter is Tuple<List<Feed>,string>)
                {
                    var data = e.Parameter as Tuple<List<Feed>, string>;
                    var feed = data.Item1;
                    AllFeed = feed;
                    TitleTextBlock.Text = data.Item2;
                    foreach (var item in feed)
                    {
                        FeedCollection.Add(item);
                    }
                }
            }
        }
        public void UpdateLayout(List<Feed> feed,string title)
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
            var item = e.ClickedItem as Feed;
            var t = new Tuple<Feed, List<Feed>>(item, FeedCollection.ToList());
            var text = AppTools.GetChildObject<TextBlock>(sender as FrameworkElement, "HeaderTitle");
            ConnectedAnimationService.GetForCurrentView().PrepareToAnimate("ForwardConnectedAnimation", text);
            MainPage.Current.MainFrame.Navigate(typeof(FeedDetailPage), t);
        }

        private async void OpenFeedButton_Click(object sender, RoutedEventArgs e)
        {
            var data = (sender as FrameworkElement).DataContext as Feed;
            if (!string.IsNullOrEmpty(data.FeedUrl))
            {
                if (data.FeedUrl.Contains("www.ithome.com"))
                {
                    string link = data.FeedUrl.Replace("https://www.ithome.com/0/", "").Replace(".htm", "");
                    link = link.Replace("/", "");
                    link = $"ithome://news?id={link}";
                    bool result = await Launcher.LaunchUriAsync(new Uri(link));
                    if (result)
                    {
                        return;
                    }
                }
                await Launcher.LaunchUriAsync(new Uri(data.FeedUrl));
            }
            else
            {
                new PopupToast(AppTools.GetReswLanguage("App_InvalidUrl"), AppTools.GetThemeSolidColorBrush(ColorType.ErrorColor)).ShowPopup();
            }
        }

        private void ShareFeedButton_Click(object sender, RoutedEventArgs e)
        {
            var data = (sender as FrameworkElement).DataContext as Feed;
            _shareData = data;
            DataTransferManager dataTransferManager = DataTransferManager.GetForCurrentView();
            dataTransferManager.DataRequested += IndexPage_DataRequested;
            DataTransferManager.ShowShareUI();
        }
        private void IndexPage_DataRequested(DataTransferManager sender, DataRequestedEventArgs args)
        {
            //创建一个数据包
            DataPackage dataPackage = new DataPackage();
            //把要分享的链接放到数据包里
            dataPackage.SetHtmlFormat(HtmlFormatHelper.CreateHtmlFormat(_shareData.Content));
            dataPackage.SetWebLink(new Uri(_shareData.FeedUrl));
            //数据包的标题（内容和标题必须提供）
            dataPackage.Properties.Title = _shareData.Title;
            //数据包的描述
            dataPackage.Properties.Description = _shareData.Summary;
            //给dataRequest对象赋值
            DataRequest request = args.Request;
            request.Data = dataPackage;
            _shareData = null;
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
