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
using Microsoft.Toolkit.Uwp.Connectivity;
using RSS_Stalker.Tools;
using Microsoft.Toolkit.Uwp.Helpers;
using Windows.UI.Xaml.Media.Animation;

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
        private List<Feed> AllFeeds = new List<Feed>();
        private Feed _shareData = null;
        public static ChannelDetailPage Current;
        private bool _isInit = false;
        public ChannelDetailPage()
        {
            this.InitializeComponent();
            NavigationCacheMode = NavigationCacheMode.Enabled;
            Current = this;
        }
        protected async override void OnNavigatedTo(NavigationEventArgs e)
        {
            if (e.NavigationMode == NavigationMode.Back)
            {
                if (FeedCollection.Count > 0 && JustNoReadSwitch.IsOn)
                {
                    foreach (var temp in MainPage.Current.ReadIds)
                    {
                        FeedCollection.Remove(FeedCollection.Where(p => p.InternalID == temp).FirstOrDefault());
                    }
                    if (FeedCollection.Count == 0)
                    {
                        AllReadTipContainer.Visibility = Visibility.Visible;
                        AllReadButton.Visibility = Visibility.Collapsed;
                    }
                }

                return;
            }
            if (e.Parameter!=null)
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
                    LoadingRing.IsActive = true;
                    LastCacheTimeContainer.Visibility = Visibility.Collapsed;
                    _sourceData = MainPage.Current.ChannelListView.SelectedItem as Channel;
                    if (_sourceData != null)
                    {
                        ChannelDescriptionTextBlock.Text = _sourceData.Description;
                        ChannelNameTextBlock.Text = _sourceData.Name;
                    }
                    await Task.Run(async () =>
                    {
                        await DispatcherHelper.ExecuteOnUIThreadAsync(async () =>
                        {
                            var feed = e.Parameter as List<Feed>;
                            AllFeeds = feed;
                            await FeedInit();
                            ChangeLayout();
                        });
                    });
                    LoadingRing.IsActive = false;
                }
            }
        }
        /// <summary>
        /// 更新布局，获取最新资讯
        /// </summary>
        /// <param name="channel">频道数据</param>
        /// <returns></returns>
        public async Task UpdateLayout(Channel channel,bool isForceRefresh=false)
        {
            AllFeeds.Clear();
            LastCacheTimeContainer.Visibility = Visibility.Collapsed;
            LoadingRing.IsActive = true;
            AllReadButton.Visibility = Visibility.Collapsed;
            JustNoReadSwitch.IsEnabled = false;
            NoDataTipContainer.Visibility = Visibility.Collapsed;
            AllReadTipContainer.Visibility = Visibility.Collapsed;
            _sourceData = channel;
            ChannelDescriptionTextBlock.Text = _sourceData.Description;
            ChannelNameTextBlock.Text = _sourceData.Name;
            FeedCollection.Clear();
            var feed = new List<Feed>();
            if (NetworkHelper.Instance.ConnectionInformation.IsInternetAvailable)
            {
                bool isCacheFirst = Convert.ToBoolean(AppTools.GetLocalSetting(AppSettings.IsCacheFirst, "False"));
                gg: if (isCacheFirst && !isForceRefresh)
                {
                    var data = await IOTools.GetLocalCache(channel);
                    feed = data.Item1;
                    int cacheTime = data.Item2;
                    int now = AppTools.DateToTimeStamp(DateTime.Now.ToLocalTime());
                    if (feed.Count == 0 || now > cacheTime + 1200)
                    {
                        isForceRefresh = true;
                        goto gg;
                    }
                    else
                    {
                        if (cacheTime > 0)
                        {
                            LastCacheTimeContainer.Visibility = Visibility.Visible;
                            LastCacheTimeBlock.Text = AppTools.TimeStampToDate(cacheTime).ToString("HH:mm");
                        }
                    }
                }
                else
                {
                    feed = await AppTools.GetFeedsFromUrl(_sourceData.Link);
                    bool isAutoCache = Convert.ToBoolean(AppTools.GetLocalSetting(AppSettings.AutoCacheWhenOpenChannel, "False"));
                    if (isAutoCache && feed.Count > 0)
                    {
                        await IOTools.AddCacheChannel(null, channel);
                    }
                }
            }
            else
            {
                if (MainPage.Current._isCacheAlert)
                {
                    new PopupToast(AppTools.GetReswLanguage("Tip_WatchingCache")).ShowPopup();
                    MainPage.Current._isCacheAlert = false;
                }
                var data= await IOTools.GetLocalCache(channel);
                feed = data.Item1;
                int cacheTime = data.Item2;
                if (cacheTime > 0)
                {
                    LastCacheTimeContainer.Visibility = Visibility.Visible;
                    LastCacheTimeBlock.Text = AppTools.TimeStampToDate(cacheTime).ToString("HH:mm");
                }
            }
            if (feed != null && feed.Count > 0)
            {
                AllFeeds = feed;
                await FeedInit();
            }
            else
            {
                NoDataTipContainer.Visibility = Visibility.Visible;
            }
            JustNoReadSwitch.IsEnabled = true;
            LoadingRing.IsActive = false;
        }
        private void FeedGridView_ItemClick(object sender, ItemClickEventArgs e)
        {
            var item = e.ClickedItem as Feed;
            var t = new Tuple<Feed, List<Feed>>(item, AllFeeds);
            var text = AppTools.GetChildObject<TextBlock>(sender as FrameworkElement, "HeaderTitle");
            ConnectedAnimationService.GetForCurrentView().PrepareToAnimate("ForwardConnectedAnimation", text);
            MainPage.Current.MainFrame.Navigate(typeof(FeedDetailPage), t,new SuppressNavigationTransitionInfo());
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

        private async void RefreshButton_Click(object sender, RoutedEventArgs e)
        {
            await UpdateLayout(_sourceData,true);
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
                AppTools.WriteRoamingSetting(AppSettings.FeedLayoutType, name);
                ChangeLayout();
            }
            
        }

        private async void JustNoReadSwitch_Toggled(object sender, RoutedEventArgs e)
        {
            if (!_isInit)
                return;
            AllReadTipContainer.Visibility = Visibility.Collapsed;
            bool isOn = JustNoReadSwitch.IsOn;
            AppTools.WriteLocalSetting(AppSettings.IsJustUnread, isOn.ToString());
            await FeedInit();
        }
        private async Task FeedInit()
        {
            if (AllFeeds.Count == 0)
            {
                NoDataTipContainer.Visibility = Visibility.Visible;
                AllReadTipContainer.Visibility = Visibility.Collapsed;
                return;
            }
            bool isJustUnread = Convert.ToBoolean(AppTools.GetLocalSetting(AppSettings.IsJustUnread, "False"));
            FeedCollection.Clear();
            await Task.Run(async () =>
            {
                await DispatcherHelper.ExecuteOnUIThreadAsync(() =>
                {
                    if (isJustUnread)
                    {
                        foreach (var item in AllFeeds)
                        {
                            if (!MainPage.Current.ReadIds.Contains(item.InternalID) && !MainPage.Current.TodoList.Any(p => p.InternalID == item.InternalID))
                            {
                                FeedCollection.Add(item);
                            }
                        }
                    }
                    else
                    {
                        foreach (var item in AllFeeds)
                        {
                            FeedCollection.Add(item);
                        }
                    }
                    bool isHasUnread = false;
                    foreach (var item in FeedCollection)
                    {
                        if (!MainPage.Current.ReadIds.Contains(item.InternalID))
                        {
                            isHasUnread = true;
                        }
                    }
                    if (FeedCollection.Count == 0)
                    {
                        AllReadTipContainer.Visibility = Visibility.Visible;
                    }
                    AllReadButton.Visibility = isHasUnread ? Visibility.Visible : Visibility.Collapsed;
                });
            });
        }
        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            bool isJustUnread = Convert.ToBoolean(AppTools.GetLocalSetting(AppSettings.IsJustUnread, "False"));
            JustNoReadSwitch.IsOn = isJustUnread;
            _isInit = true;
        }

        private async void AllReadButton_Click(object sender, RoutedEventArgs e)
        {
            var list = new List<string>();
            foreach (var item in AllFeeds)
            {
                list.Add(item.InternalID);
            }
            MainPage.Current.AddReadId(list.ToArray());
            AllReadButton.Visibility = Visibility.Collapsed;
            bool isJustUnread = Convert.ToBoolean(AppTools.GetLocalSetting(AppSettings.IsJustUnread, "False"));
            if(isJustUnread)
                await FeedInit();
        }

        private void Page_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (MainPage.Current.MinsizeHeaderContainer.Visibility == Visibility.Visible)
            {
                ChannelDescriptionTextBlock.Visibility = Visibility.Collapsed;
            }
            else
            {
                ChannelDescriptionTextBlock.Visibility = Visibility.Visible;
            }
        }
    }
}
