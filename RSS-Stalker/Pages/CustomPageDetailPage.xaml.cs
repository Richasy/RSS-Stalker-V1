using CoreLib.Enums;
using CoreLib.Models;
using CoreLib.Models.App;
using CoreLib.Tools;
using Microsoft.Toolkit.Uwp.Connectivity;
using Microsoft.Toolkit.Uwp.Helpers;
using Rss.Parsers.Rss;
using RSS_Stalker.Controls;
using RSS_Stalker.Tools;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media.Animation;
using Windows.UI.Xaml.Navigation;

// https://go.microsoft.com/fwlink/?LinkId=234238 上介绍了“空白页”项模板

namespace RSS_Stalker.Pages
{
    /// <summary>
    /// 可用于自身或导航至 Frame 内部的空白页。
    /// </summary>
    public sealed partial class CustomPageDetailPage : Page
    {
        public CustomPage _sourceData = null;
        private ObservableCollection<RssSchema> FeedCollection = new ObservableCollection<RssSchema>();
        private List<RssSchema> AllFeeds = new List<RssSchema>();
        public static CustomPageDetailPage Current;
        private bool _isInit = false;
        private int _lastCacheTime = 0;
        public CustomPageDetailPage()
        {
            this.InitializeComponent();
            NavigationCacheMode = NavigationCacheMode.Enabled;
            Current = this;
        }
        protected async override void OnNavigatedTo(NavigationEventArgs e)
        {
            if (e.NavigationMode == NavigationMode.Back)
            {
                //if (FeedCollection.Count > 0 && JustNoReadSwitch.IsOn)
                //{
                //    foreach (var temp in MainPage.Current.ReadIds)
                //    {
                //        FeedCollection.Remove(FeedCollection.Where(p => p.InternalID == temp).FirstOrDefault());
                //    }
                //    if (FeedCollection.Count == 0)
                //    {
                //        AllReadTipContainer.Visibility = Visibility.Visible;
                //        AllReadButton.Visibility = Visibility.Collapsed;
                //    }
                //}
                return;
            }
            if (e.Parameter != null)
            {
                NoDataTipContainer.Visibility = Visibility.Collapsed;
                // 当传入源为频道数据时（获取当前频道最新资讯）
                if (e.Parameter is CustomPage)
                {
                    try
                    {
                        await UpdateLayout(e.Parameter as CustomPage);
                    }
                    catch (Exception)
                    {
                        NoDataTipContainer.Visibility = Visibility.Visible;
                        LoadingRing.IsActive = false;
                    }
                    finally
                    {
                        ChangeLayout();
                    }
                    
                }
                // 当传入源为文章列表时（说明是上一级返回，不获取最新资讯）
                else if (e.Parameter is List<RssSchema>)
                {
                    LastCacheTimeContainer.Visibility = Visibility.Collapsed;
                    _sourceData = MainPage.Current.PageListView.SelectedItem as CustomPage;
                    if (_sourceData != null)
                    {
                        PageNameTextBlock.Text = _sourceData.Name;
                    }
                    LoadingRing.IsActive = true;
                    await Task.Run(async() =>
                    {
                        await DispatcherHelper.ExecuteOnUIThreadAsync(async () =>
                        {
                            AllFeeds = e.Parameter as List<RssSchema>;
                            await FeedInit();
                            ChangeLayout();
                        });
                    });
                    LoadingRing.IsActive = false;
                }
            }
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
                            if (!MainPage.Current.ReadIds.Contains(item.InternalID) && !MainPage.Current.TodoList.Any(p=>p.InternalID==item.InternalID))
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
        /// <summary>
        /// 更新布局，获取最新资讯
        /// </summary>
        /// <param name="page">频道数据</param>
        /// <returns></returns>
        public async Task UpdateLayout(CustomPage page,bool isForceRefresh=false)
        {
            AllFeeds.Clear();
            LoadingRing.IsActive = true;
            JustNoReadSwitch.IsEnabled = false;
            AllReadButton.Visibility = Visibility.Collapsed;
            LastCacheTimeContainer.Visibility = Visibility.Collapsed;
            NoDataTipContainer.Visibility = Visibility.Collapsed;
            AllReadTipContainer.Visibility = Visibility.Collapsed;
            _sourceData = page;
            PageNameTextBlock.Text = _sourceData.Name;
            FeedCollection.Clear();
            var feed = new List<RssSchema>();
            if (NetworkHelper.Instance.ConnectionInformation.IsInternetAvailable)
            {
                var schema = await IOTools.GetSchemaFromPage(_sourceData);
                foreach (var item in schema)
                {
                    feed.Add(item);
                }
                bool isAutoCache = Convert.ToBoolean(AppTools.GetLocalSetting(AppSettings.AutoCacheWhenOpenChannel, "False"));
                if (isAutoCache && feed.Count > 0)
                {
                    await IOTools.AddCachePage(null, page);
                }

            }
            else
            {
                if (MainPage.Current._isCacheAlert)
                {
                    new PopupToast(AppTools.GetReswLanguage("Tip_WatchingCache")).ShowPopup();
                    MainPage.Current._isCacheAlert = false;
                }
                var data = await IOTools.GetLocalCache(page);
                feed = data.Item1;
                _lastCacheTime = data.Item2;
                if (_lastCacheTime > 0)
                {
                    LastCacheTimeContainer.Visibility = Visibility.Visible;
                    LastCacheTimeBlock.Text = AppTools.TimeStampToDate(_lastCacheTime).ToString("HH:mm");
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
            var item = e.ClickedItem as RssSchema;
            var t = new Tuple<RssSchema, List<RssSchema>>(item, AllFeeds);
            var text = AppTools.GetChildObject<TextBlock>(sender as FrameworkElement, "TitleBlock");
            ConnectedAnimationService.GetForCurrentView().PrepareToAnimate("ForwardConnectedAnimation", text);
            MainPage.Current.MainFrame.Navigate(typeof(FeedDetailPage), t,new SuppressNavigationTransitionInfo());
        }

        

        private async void RefreshButton_Click(object sender, RoutedEventArgs e)
        {
            await UpdateLayout(_sourceData, true);
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
            string oldLayout = AppTools.GetRoamingSetting(AppSettings.FeedLayoutType, "All");
            if (oldLayout != name)
            {
                AppTools.WriteRoamingSetting(AppSettings.FeedLayoutType, name);
                ChangeLayout();
            }

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
            if (isJustUnread)
                await FeedInit();
        }

        private void Page_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (MainPage.Current.MinsizeHeaderContainer.Visibility == Visibility.Visible)
            {
                HeaderContainer.Height = 50;
            }
            else
            {
                HeaderContainer.Height = 70;
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

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            bool isJustUnread = Convert.ToBoolean(AppTools.GetLocalSetting(AppSettings.IsJustUnread, "False"));
            JustNoReadSwitch.IsOn = isJustUnread;
            _isInit = true;
        }
    }
}
