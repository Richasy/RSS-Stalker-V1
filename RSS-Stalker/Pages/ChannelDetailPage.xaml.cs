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

// https://go.microsoft.com/fwlink/?LinkId=234238 上介绍了“空白页”项模板

namespace RSS_Stalker.Pages
{
    /// <summary>
    /// 可用于自身或导航至 Frame 内部的空白页。
    /// </summary>
    public sealed partial class ChannelDetailPage : Page
    {
        public Channel _sourceData = null;
        private ObservableCollection<Feed> FeedCollection = new ObservableCollection<Feed>();
        private Feed _shareData = null;
        public static ChannelDetailPage Current;
        
        public ChannelDetailPage()
        {
            this.InitializeComponent();
            Current = this;
        }
        protected async override void OnNavigatedTo(NavigationEventArgs e)
        {
            if(e.Parameter!=null)
            {
                // 当传入源为频道数据时（获取当前频道最新资讯）
                if(e.Parameter is Channel)
                {
                    await UpdateLayout(e.Parameter as Channel);
                    ChangeLayout();
                }
                // 当传入源为文章列表时（说明是上一级返回，不获取最新资讯）
                else if(e.Parameter is List<Feed>)
                {
                    var feed = e.Parameter as List<Feed>;
                    _sourceData = MainPage.Current.ChannelListView.SelectedItem as Channel;
                    if (_sourceData != null)
                    {
                        ChannelDescriptionTextBlock.Text = _sourceData.Description;
                        ChannelNameTextBlock.Text = _sourceData.Name;
                        foreach (var item in feed)
                        {
                            FeedCollection.Add(item);
                        }
                    }
                    ChangeLayout();
                }
            }
        }
        /// <summary>
        /// 更新布局，获取最新资讯
        /// </summary>
        /// <param name="channel">频道数据</param>
        /// <returns></returns>
        public async Task UpdateLayout(Channel channel)
        {
            LoadingRing.IsActive = true;
            NoDataTipContainer.Visibility = Visibility.Collapsed;
            _sourceData = channel;
            ChannelDescriptionTextBlock.Text = _sourceData.Description;
            ChannelNameTextBlock.Text = _sourceData.Name;
            FeedCollection.Clear();
            var feed = await AppTools.GetFeedsFromUrl(_sourceData.Link);
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
            MainPage.Current.MainFrame.Navigate(typeof(FeedDetailPage), t);
        }

        private async void OpenChannelButton_Click(object sender, RoutedEventArgs e)
        {
            if (!string.IsNullOrEmpty(_sourceData.SourceUrl))
            {
                await Launcher.LaunchUriAsync(new Uri(_sourceData.SourceUrl));
            }
            else
            {
                new PopupToast(AppTools.GetReswLanguage("App_InvalidUrl"),AppTools.GetThemeSolidColorBrush(ColorType.ErrorColor)).ShowPopup();
            }
        }

        private async void OpenFeedButton_Click(object sender, RoutedEventArgs e)
        {
            var data = (sender as FrameworkElement).DataContext as Feed;
            if (!string.IsNullOrEmpty(data.FeedUrl))
            {
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

        private async void RefreshButton_Click(object sender, RoutedEventArgs e)
        {
            await UpdateLayout(_sourceData);
        }
        private void ChangeLayout()
        {
            string name = AppTools.GetRoamingSetting(AppSettings.FeedLayoutType, "All");
            if (name == "All")
            {
                FeedGridView.Visibility = Visibility.Visible;
                FeedListView.Visibility = Visibility.Collapsed;
                FeedGridView.ItemTemplate = FeedWaterfallItemTemplate;
                Waterfall.IsChecked = true;
            }
            else if (name == "Card")
            {
                FeedGridView.Visibility = Visibility.Visible;
                FeedListView.Visibility = Visibility.Collapsed;
                FeedGridView.ItemTemplate = FeedCardItemTemplate;
                Card.IsChecked = true;
            }
            else
            {
                FeedGridView.Visibility = Visibility.Collapsed;
                FeedListView.Visibility = Visibility.Visible;
                List.IsChecked = true;
            }
        }
        private void LayoutMenuItem_Click(object sender, RoutedEventArgs e)
        {
            string name = (sender as ToggleMenuFlyoutItem).Name;
            switch (name)
            {
                case "Waterfall":
                    // 由于迭代的问题，这里不好修改，就定为All了
                    name = "All";
                    Card.IsChecked = false;
                    List.IsChecked = false;
                    break;
                case "Card":
                    Waterfall.IsChecked = false;
                    List.IsChecked = false;
                    break;
                case "List":
                    Card.IsChecked = false;
                    Waterfall.IsChecked = false;
                    break;
                default:
                    name = "All";
                    break;
            }
            string oldLayout = AppTools.GetRoamingSetting(CoreLib.Enums.AppSettings.FeedLayoutType, "All");
            if (oldLayout != name)
            {
                AppTools.WriteRoamingSetting(CoreLib.Enums.AppSettings.FeedLayoutType, name);
                ChangeLayout();
            }
            
        }
    }
}
