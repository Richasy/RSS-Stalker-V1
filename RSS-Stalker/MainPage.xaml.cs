using RSS_Stalker.Controls;
using RSS_Stalker.Dialog;
using CoreLib.Models;
using CoreLib.Tools;
using CoreLib.Enums;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Navigation;
using RSS_Stalker.Tools;
using Windows.ApplicationModel.Background;
using Microsoft.Toolkit.Uwp.Helpers;
using Windows.UI.Core;
using System.Threading.Tasks;
using Windows.Storage;
using Windows.ApplicationModel;
using Microsoft.Toolkit.Uwp.Connectivity;
using CoreLib.Models.App;
using Windows.UI.Xaml.Media.Imaging;
using System.Text;
using Rss.Parsers.Rss;

// https://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x804 上介绍了“空白页”项模板

namespace RSS_Stalker
{
    /// <summary>
    /// 可用于自身或导航至 Frame 内部的空白页。
    /// </summary>
    public sealed partial class MainPage : Page
    {
        public ObservableCollection<Category> Categories = new ObservableCollection<Category>();
        public ObservableCollection<Channel> Channels = new ObservableCollection<Channel>();
        public ObservableCollection<CustomPage> CustomPages = new ObservableCollection<CustomPage>();
        public List<SystemFont> SystemFonts = new List<SystemFont>();
        public List<RssSchema> TodoList = new List<RssSchema>();
        public List<RssSchema> StarList = new List<RssSchema>();
        public List<Channel> ToastList = new List<Channel>();
        public List<Channel> ReadableList = new List<Channel>();
        public List<CacheModel> TempCache = new List<CacheModel>();
        public List<string> ReadIds = new List<string>();
        public bool _isFromTimeline = false;
        public static MainPage Current;
        public bool _isCacheAlert = false;
        /// <summary>
        /// 分类列表数量标识，在进行数据替换和拖放排序时作为参照。在清空列表前，一定要将该标识设为-1
        /// </summary>
        public int _pageListCount = -1;
        /// <summary>
        /// 分类列表数量标识，在进行数据替换和拖放排序时作为参照。在清空列表前，一定要将该标识设为-1
        /// </summary>
        public int _categoryListCount = -1;
        /// <summary>
        /// 频道列表数量标识，在进行数据替换和拖放排序时作为参照。在清空列表前，一定要将该标识设为-1
        /// </summary>
        public int _channelListCount = -1;
        /// <summary>
        /// 检测同步的计时器（由于RoamingSettings的不即时性，当前已弃用该功能）
        /// </summary>
        private DispatcherTimer _checkUpdateTimer=new DispatcherTimer();
        private bool _isInit = false;
        private bool _isTodoButtonClick = false;
        public MainPage()
        {
            this.InitializeComponent();
            Current = this;
            Window.Current.SetTitleBar(TitleBarControl);
            PageInit();
        }
        /// <summary>
        /// 分类排序时的检测
        /// 基本原理就是，若当前数据源的数量发生变化时（拖放排序本质上是先删除后插入），进行数据跟踪，若数据集合的数目
        /// 最终与标识符一致时，判定为排序完成，此时记录当前列表的顺序，并进行数据同步
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void CategoryCollectionReordered(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (!_isInit)
            {
                return;
            }
            var list = Categories.ToList();
            if (list.Count == _categoryListCount)
            {
                await IOTools.ReplaceCategory(list,true);
            }
        }
        private async void ChannelCollectionReordered(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (!_isInit)
            {
                return;
            }
            var list = Channels.ToList();
            if (list.Count == _channelListCount)
            {
                var selectCategory = CategoryListView.SelectedItem as Category;
                if (selectCategory != null)
                {
                    selectCategory.Channels = list;
                }
                await IOTools.ReplaceCategory(Categories.ToList(), true);
            }
        }
        private async void PageCollectionReordered(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (!_isInit)
            {
                return;
            }
            var list = CustomPages.ToList();
            if (list.Count == _pageListCount)
            {
                await IOTools.ReplacePage(list, true);
            }
        }
        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            if(e.Parameter !=null && e.Parameter is string)
            {
                string type = e.Parameter as string;
                if(type=="Timeline")
                    _isFromTimeline = true;
            }
        }
        /// <summary>
        /// 页面准备
        /// </summary>
        private async void PageInit()
        {
            AppTools.SetTitleBarColor();
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
            LoadingRing.IsActive = true;
            string theme = AppTools.GetRoamingSetting(AppSettings.Theme, "Light");
            var icon = new BitmapImage();
            bool isHidePage = Convert.ToBoolean(AppTools.GetLocalSetting(AppSettings.IsHidePage, "False"));
            if (isHidePage)
                PageContainer.Visibility = Visibility.Collapsed;
            else
                PageContainer.Visibility = Visibility.Visible;
            icon.UriSource = new Uri($"ms-appx:///Assets/{theme}.png");
            AppIcon.Source = icon;
            bool isOneDrive = Convert.ToBoolean(AppTools.GetLocalSetting(AppSettings.IsBindingOneDrive, "False"));
            AppTitleBlock.Text = AppTools.GetReswLanguage("DisplayName");
            await OtherListInit();
            // 监听集合变化
            Categories.CollectionChanged += CategoryCollectionReordered;
            Channels.CollectionChanged += ChannelCollectionReordered;
            CustomPages.CollectionChanged += PageCollectionReordered;
            // 获取本地保存的订阅源副本
            var categories = await IOTools.GetLocalCategories();
            var pages = await IOTools.GetLocalPages();
            ReadIds = await IOTools.GetReadIds();
            // 清空列表前将标识符设置为-1
            _categoryListCount = -1;
            _channelListCount = -1;
            _pageListCount = -1;
            Channels.Clear();
            Categories.Clear();
            CustomPages.Clear();
            foreach (var item in categories)
            {
                Categories.Add(item);
            }
            foreach (var item in pages)
            {
                CustomPages.Add(item);
            }
            if (Categories.Count == 0)
            {
                CategoryNameTextBlock.Text = "RSS Stalker";
                SideChannelGrid.Visibility = Visibility.Collapsed;
                AppSplitView.OpenPaneLength = 250;
                MainFrame.Navigate(typeof(Pages.WelcomePage));
            }
            else
            {
                if(!_isFromTimeline)
                    SelectChannelByCustom();
                else
                {
                    CategoryNameTextBlock.Text = "RSS Stalker";
                    SideChannelGrid.Visibility = Visibility.Collapsed;
                    PageListView.SelectedIndex = -1;
                    CategoryListView.SelectedIndex = -1;
                    AppSplitView.OpenPaneLength = 250;
                }
            }
            // 在完成列表装载后，将列表的数量重新赋值给标识符
            _categoryListCount = Categories.Count;
            _channelListCount = Channels.Count;
            LoadingRing.IsActive = false;
            var tempTasks = new Task[3];
            tempTasks[0] = Task.Run(async () =>
            {
                await DispatcherHelper.ExecuteOnUIThreadAsync(async () =>
                {
                    if (isOneDrive && NetworkHelper.Instance.ConnectionInformation.IsInternetAvailable)
                        await App.OneDrive.OneDriveAuthorize();
                    await IOTools.UpdateAllListToOneDrive();
                });
            });
            tempTasks[1] = Task.Run(async () =>
            {
                await DispatcherHelper.ExecuteOnUIThreadAsync(async () =>
                {
                    bool isShowNoRead = Convert.ToBoolean(AppTools.GetLocalSetting(AppSettings.IsShowNoRead, "True"));
                    if (isShowNoRead)
                        await CacheAll();
                });
            });
            tempTasks[2] = Task.Run(async () =>
            {
                await DispatcherHelper.ExecuteOnUIThreadAsync(async () =>
                {
                    SystemFonts = SystemFont.GetFonts();
                    await CheckVersion();
                });

            });
            // 完成OneDrive的数据链接
            
            // TimerInit();
            
            // 注册后台
            RegisterBackground();
            // 注册快捷键
            Window.Current.Dispatcher.AcceleratorKeyActivated += AccelertorKeyActivedHandle;
            await Task.WhenAll(tempTasks);
            _isInit = true;
            // 检查版本更新
            
        }
        private async Task OtherListInit()
        {
            var tasks = new List<Task>();
            tasks.Add(Task.Run(async () =>
            {
                TodoList = await IOTools.GetLocalTodoReadList();
            }));
            tasks.Add(Task.Run(async () =>
            {
                StarList = await IOTools.GetLocalStarList();
            }));
            tasks.Add(Task.Run(async () =>
            {
                ToastList = await IOTools.GetNeedToastChannels();
            }));
            tasks.Add(Task.Run(async () =>
            {
                ReadableList = await IOTools.GetNeedReadableChannels();
            }));
            await Task.WhenAll(tasks.ToArray());
        }
        public void SideHide()
        {
            PageListView.SelectedIndex = -1;
            CategoryListView.SelectedIndex = -1;
            SideChannelGrid.Visibility = Visibility.Collapsed;
            AppSplitView.OpenPaneLength = 250;
        }
        /// <summary>
        /// 快捷键注册
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="args"></param>
        private async void AccelertorKeyActivedHandle(CoreDispatcher sender, AcceleratorKeyEventArgs args)
        {
            if (args.EventType.ToString().Contains("Down"))
            {
                var esc = Window.Current.CoreWindow.GetKeyState(Windows.System.VirtualKey.Escape);
                var left = Window.Current.CoreWindow.GetKeyState(Windows.System.VirtualKey.Left);
                var right = Window.Current.CoreWindow.GetKeyState(Windows.System.VirtualKey.Right);
                var ctrl = Window.Current.CoreWindow.GetKeyState(Windows.System.VirtualKey.Control);
                var shift = Window.Current.CoreWindow.GetKeyState(Windows.System.VirtualKey.Shift);
                if (esc.HasFlag(CoreVirtualKeyStates.Down))
                {
                    if (MainFrame.Content is Pages.FeedDetailPage)
                    {
                        Pages.FeedDetailPage.Current.CheckBack();
                    }
                }
                else if (left.HasFlag(CoreVirtualKeyStates.Down))
                {
                    if (MainFrame.Content is Pages.FeedDetailPage)
                    {
                        Pages.FeedDetailPage.Current.PreviousArticle();
                    }
                }
                else if (right.HasFlag(CoreVirtualKeyStates.Down))
                {
                    if (MainFrame.Content is Pages.FeedDetailPage)
                    {
                        Pages.FeedDetailPage.Current.NextArticle();
                    }
                }
                else if (shift.HasFlag(CoreVirtualKeyStates.Down))
                {
                    if (args.VirtualKey == Windows.System.VirtualKey.R)
                    {
                        await CacheAll();
                    }
                    else if(args.VirtualKey==Windows.System.VirtualKey.A)
                    {
                        if(MainFrame.Content is Pages.ChannelDetailPage)
                        {
                            await Pages.ChannelDetailPage.Current.AllRead();
                        }
                        else if(MainFrame.Content is Pages.CustomPageDetailPage)
                        {
                            await Pages.CustomPageDetailPage.Current.AllRead();
                        }
                    }
                }
                else if (ctrl.HasFlag(CoreVirtualKeyStates.Down))
                {
                    if (args.VirtualKey == Windows.System.VirtualKey.B)
                    {
                        if(MainFrame.Content is Pages.FeedDetailPage)
                        {
                            await Pages.FeedDetailPage.Current.OpenWeb();
                        }
                    }
                }
            }
        }
        /// <summary>
        /// 后台注册
        /// </summary>
        private void RegisterBackground()
        {
            BackgroundTaskHelper.Register(typeof(StalkerToast.Toast),
                                    new TimeTrigger(15, true),
                                    false, true,
                                    new SystemCondition(SystemConditionType.InternetAvailable));
            BackgroundTaskHelper.Register(typeof(AutoCache.Main),
                                    new TimeTrigger(15, true),
                                    false, true,
                                    new SystemCondition(SystemConditionType.FreeNetworkAvailable));
        }
        /// <summary>
        /// 暂时弃用
        /// </summary>
        private void TimerInit()
        {
            _checkUpdateTimer.Interval = new TimeSpan(0, 0, 30);
            _checkUpdateTimer.Tick += CheckRssListUpdate;
            _checkUpdateTimer.Start();
        }
        /// <summary>
        /// 检查同步，暂时弃用
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void CheckRssListUpdate(object sender, object e)
        {
            string localBasicTime = AppTools.GetLocalSetting(AppSettings.BasicUpdateTime, "0");
            string roamBasicTime = AppTools.GetRoamingSetting(AppSettings.BasicUpdateTime,"1");
            string localTodoTime = AppTools.GetLocalSetting(AppSettings.TodoUpdateTime, "0");
            string roamTodoTime = AppTools.GetRoamingSetting(AppSettings.TodoUpdateTime, "1");
            string localStarTime = AppTools.GetLocalSetting(AppSettings.StarUpdateTime, "0");
            string roamStarTime = AppTools.GetRoamingSetting(AppSettings.StarUpdateTime, "1");
            string localToastTime = AppTools.GetLocalSetting(AppSettings.ToastUpdateTime, "0");
            string roamToastTime = AppTools.GetRoamingSetting(AppSettings.ToastUpdateTime, "1");
            if (localBasicTime != roamBasicTime)
            {
                var list = await App.OneDrive.GetCategoryList();
                await IOTools.ReplaceCategory(list);
                ReplaceList(list);
                AppTools.WriteLocalSetting(AppSettings.BasicUpdateTime, roamBasicTime);
                new PopupToast(AppTools.GetReswLanguage("Tip_Updated")).ShowPopup();
            }
            if (localTodoTime != roamTodoTime)
            {
                var list = await App.OneDrive.GetTodoList();
                await IOTools.ReplaceTodo(list);
                TodoList = list;
                // 如果正在浏览待读页，则替换
                if(MainFrame.Content is Pages.FeedCollectionPage && _isTodoButtonClick)
                {
                    Pages.FeedCollectionPage.Current.UpdateLayout(TodoList, AppTools.GetReswLanguage("Tip_Todo"));
                }
                AppTools.WriteLocalSetting(AppSettings.TodoUpdateTime, roamTodoTime);
                new PopupToast(AppTools.GetReswLanguage("Tip_Updated")).ShowPopup();
            }
            if (localStarTime != roamStarTime)
            {
                var list = await App.OneDrive.GetStarList();
                StarList = list;
                await IOTools.ReplaceStar(list);
                if (MainFrame.Content is Pages.FeedCollectionPage && !_isTodoButtonClick)
                {
                    Pages.FeedCollectionPage.Current.UpdateLayout(StarList, AppTools.GetReswLanguage("Tip_Star"));
                }
                AppTools.WriteLocalSetting(AppSettings.StarUpdateTime, roamStarTime);
                // 如果正在浏览待读页，则替换
                new PopupToast(AppTools.GetReswLanguage("Tip_Updated")).ShowPopup();
            }
            if (localToastTime != roamToastTime)
            {
                var list = await App.OneDrive.GetToastList();
                ToastList = list;
                await IOTools.ReplaceToast(list);
                if (MainFrame.Content is Pages.SettingPage)
                {
                    Pages.SettingPage.Current.ToastChannels.Clear();
                    foreach (var item in ToastList)
                    {
                        Pages.SettingPage.Current.ToastChannels.Add(item);
                    }
                }
                AppTools.WriteLocalSetting(AppSettings.ToastUpdateTime, roamToastTime);
                // 如果正在浏览待读页，则替换
                new PopupToast(AppTools.GetReswLanguage("Tip_Updated")).ShowPopup();
            }
        }
        /// <summary>
        /// 替换整个分类及频道列表，替换完成后，恢复之前的状态
        /// </summary>
        /// <param name="list">分类列表</param>
        public void ReplaceList(List<Category> list)
        {
            string roamTime = AppTools.GetRoamingSetting(AppSettings.BasicUpdateTime, "1");
            
            string selectCategoryId = (CategoryListView.SelectedItem as Category)?.Id;
            string selectChannelId = (ChannelListView.SelectedItem as Channel)?.Id;
            _categoryListCount = -1;
            Categories.Clear();
            foreach (var item in list)
            {
                Categories.Add(item);
            }
            if (!string.IsNullOrEmpty(selectCategoryId))
            {
                var selectCategory = Categories.Where(p => p.Id == selectCategoryId).FirstOrDefault();
                if (selectCategory != null)
                {
                    _channelListCount = -1;
                    CategoryListView.SelectedItem = selectCategory;
                    Channels.Clear();
                    foreach (var cha in selectCategory.Channels)
                    {
                        Channels.Add(cha);
                        if (!string.IsNullOrEmpty(selectChannelId))
                        {
                            var selectChannel = Channels.Where(c => c.Id == selectChannelId).FirstOrDefault();
                            if (selectChannel != null)
                                ChannelListView.SelectedItem = selectChannel;
                        }
                    }
                }
            }
            
            
            _categoryListCount = Categories.Count;
            _channelListCount = Channels.Count;
            AppTools.WriteLocalSetting(AppSettings.BasicUpdateTime, roamTime);
        }
        public void ReplacePageList(List<CustomPage> pages)
        {
            string roamTime = AppTools.GetRoamingSetting(AppSettings.PageUpdateTime, "1");
            string selectPageId = (PageListView.SelectedItem as CustomPage)?.Id;
            _pageListCount = -1;
            CustomPages.Clear();
            foreach (var item in pages)
            {
                CustomPages.Add(item);
            }
            if (!string.IsNullOrEmpty(selectPageId))
            {
                var selectPage = CustomPages.Where(p => p.Id == selectPageId).FirstOrDefault();
                if (selectPage != null)
                {
                    PageListView.SelectedItem = selectPage;
                }
            }
            _pageListCount = CustomPages.Count;
            AppTools.WriteLocalSetting(AppSettings.PageUpdateTime, roamTime);
        }
        /// <summary>
        /// 离开页面前注销计时器
        /// </summary>
        /// <param name="e"></param>
        protected override void OnNavigatedFrom(NavigationEventArgs e)
        {
            //_checkUpdateTimer.Stop();
            _checkUpdateTimer = null;
            base.OnNavigatedFrom(e);
        }
        /// <summary>
        /// 检查版本更新，并弹出更新通告
        /// </summary>
        /// <returns></returns>
        private async Task CheckVersion()
        {
            try
            {
                string localVersion = AppTools.GetLocalSetting(AppSettings.AppVersion, "");
                string nowVersion = string.Format("{0}.{1}.{2}.{3}", Package.Current.Id.Version.Major, Package.Current.Id.Version.Minor, Package.Current.Id.Version.Build, Package.Current.Id.Version.Revision);
                string lan = AppTools.GetRoamingSetting(AppSettings.Language,"en_US");
                if (localVersion != nowVersion)
                {
                    var updateFile = await StorageFile.GetFileFromApplicationUriAsync(new Uri($"ms-appx:///{lan}.txt"));
                    string updateInfo = await FileIO.ReadTextAsync(updateFile);
                    await new ConfirmDialog(AppTools.GetReswLanguage("Tip_UpdateTip"), updateInfo).ShowAsync();
                    AppTools.WriteLocalSetting(AppSettings.AppVersion, nowVersion);
                }
            }
            catch (Exception)
            {
                return;
            }
            
        }
        private void CategoryListView_ItemClick(object sender, ItemClickEventArgs e)
        {
            var item = e.ClickedItem as Category;
            TodoButton.IsChecked = false;
            StarButton.IsChecked = false;
            SettingButton.IsChecked = false;
            CategoryNameTextBlock.Text = item.Name;
            ChannelListView.SelectedIndex = -1;
            PageListView.SelectedIndex = -1;
            SideChannelGrid.Visibility = Visibility.Visible;
            AppSplitView.OpenPaneLength = 550;
            _channelListCount = -1;
            Channels.Clear();
            foreach (var cha in item.Channels)
            {
                Channels.Add(cha);
            }
            _channelListCount = Channels.Count;
            if (!(MainFrame.Content is Pages.WelcomePage))
                MainFrame.Navigate(typeof(Pages.WelcomePage));
        }

        private async void ChannelListView_ItemClick(object sender, ItemClickEventArgs e)
        {
            var item = e.ClickedItem as Channel;
            if (MinsizeHeaderContainer.Visibility == Visibility.Visible)
            {
                AppSplitView.IsPaneOpen = false;
            }
            if(MainFrame.Content is Pages.ChannelDetailPage)
            {
                await Pages.ChannelDetailPage.Current.UpdateLayout(item);
            }
            else
            {
                MainFrame.Navigate(typeof(Pages.ChannelDetailPage), item);
            }
        }

        private async void AddCategoryButton_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new AddCategoryDialog();
            await dialog.ShowAsync();
        }

        private void ChannelSearchBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            string text = ChannelSearchBox.Text?.Trim();
            var selectCategory = CategoryListView.SelectedItem as Category;
            if (!string.IsNullOrEmpty(text))
            {
                if (selectCategory != null)
                {
                    var list = selectCategory.Channels.Where(p => AppTools.NormalString(p.Name).IndexOf(AppTools.NormalString(text)) != -1).ToList();
                    _channelListCount = -1;
                    Channels.Clear();
                    foreach (var item in list)
                    {
                        Channels.Add(item);
                    }
                    _channelListCount = list.Count;
                }
            }
            else
            {
                _channelListCount = -1;
                Channels.Clear();
                if (selectCategory != null)
                {
                    foreach (var item in selectCategory.Channels)
                    {
                        Channels.Add(item);
                    }
                    _channelListCount = selectCategory.Channels.Count;
                }
                else
                    _channelListCount = 0;
            }
        }

        private async void AddChannelButton_Click(object sender, RoutedEventArgs e)
        {
            if (!NetworkHelper.Instance.ConnectionInformation.IsInternetAvailable)
            {
                new PopupToast(AppTools.GetReswLanguage("Tip_FailedWithoutInternet"),AppTools.GetThemeSolidColorBrush(ColorType.ErrorColor)).ShowPopup();
                return;
            }
            var dialog = new AddChannelDialog();
            await dialog.ShowAsync();
        }

        private void SettingButton_Click(object sender, RoutedEventArgs e)
        {
            SideChannelGrid.Visibility = Visibility.Collapsed;
            if (MinsizeHeaderContainer.Visibility == Visibility.Visible)
            {
                AppSplitView.IsPaneOpen = false;
            }
            AppSplitView.OpenPaneLength = 250;
            CategoryListView.SelectedIndex = -1;
            TodoButton.IsChecked = false;
            StarButton.IsChecked = false;
            PageListView.SelectedIndex = -1;
            MainFrame.Navigate(typeof(Pages.SettingPage));
            SettingButton.IsChecked = true;
        }

        private async void UpdateChannelMenuItem_Click(object sender, RoutedEventArgs e)
        {
            var data = (sender as FrameworkElement).DataContext as Channel;
            var dialog = new ModifyChannelDialog(data);
            await dialog.ShowAsync();
        }
        /// <summary>
        /// 删除当前频道
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void DeleteChannelMenuItem_Click(object sender, RoutedEventArgs e)
        {
            var data = (sender as FrameworkElement).DataContext as Channel;
            var confirmDialog = new ConfirmDialog(AppTools.GetReswLanguage("Tip_DeleteWarning"), AppTools.GetReswLanguage("Tip_DeleteChannelWarning"));
            confirmDialog.PrimaryButtonClick += async (_s, _e) =>
            {
                _e.Cancel = true;
                var category = CategoryListView.SelectedItem as Category;
                var selectChannel = ChannelListView.SelectedItem as Channel;
                if (category != null)
                {
                    if(selectChannel!=null && selectChannel.Id == data.Id)
                    {
                        MainFrame.Navigate(typeof(Pages.WelcomePage));
                    }
                    confirmDialog.IsPrimaryButtonEnabled = false;
                    confirmDialog.PrimaryButtonText = AppTools.GetReswLanguage("Tip_Waiting");
                    category.Channels.RemoveAll(p => p.Id == data.Id);
                    await IOTools.UpdateCategory(category);
                    Channels.Remove(data);
                    _channelListCount -= 1;
                    new PopupToast(AppTools.GetReswLanguage("Tip_DeleteChannelSuccess")).ShowPopup();
                    data = null;
                    confirmDialog.Hide();
                }
                else
                {
                    new PopupToast(AppTools.GetReswLanguage("Tip_NoCategorySelected"), AppTools.GetThemeSolidColorBrush(ColorType.ErrorColor)).ShowPopup();
                }
            };
            await confirmDialog.ShowAsync();
        }
        private void UpdatePageMenuItem_Click(object sender, RoutedEventArgs e)
        {
            var data = (sender as FrameworkElement).DataContext as CustomPage;
            CloseCategory();
            MainFrame.Navigate(typeof(Pages.OperaterCustomPage), data);
        }
        private async void CachePageMenuItem_Click(object sender, RoutedEventArgs e)
        {
            LoadingRing.IsActive = true;
            var data = (sender as FrameworkElement).DataContext as CustomPage;
            CacheProgressBar.Visibility = Visibility.Visible;
            try
            {
                CacheProgressBar.Maximum = 1;
                await IOTools.AddCachePage(async (count) => {
                    await DispatcherHelper.ExecuteOnUIThreadAsync(() =>
                    {
                        CacheProgressBar.Value = count;
                    });
                },
                    data);
                new PopupToast(AppTools.GetReswLanguage("Tip_CacheSuccess")).ShowPopup();
            }
            catch (Exception)
            {
                new PopupToast(AppTools.GetReswLanguage("Tip_CacheFailed"), AppTools.GetThemeSolidColorBrush(ColorType.ErrorColor)).ShowPopup();
            }
            CacheProgressBar.Visibility = Visibility.Collapsed;
            LoadingRing.IsActive = false;
        }

        private async void DeletePageMenuItem_Click(object sender, RoutedEventArgs e)
        {
            var data = (sender as FrameworkElement).DataContext as CustomPage;
            var confirmDialog = new ConfirmDialog(AppTools.GetReswLanguage("Tip_DeleteWarning"), AppTools.GetReswLanguage("Tip_DeletePageWarning"));
            confirmDialog.PrimaryButtonClick += async (_s, _e) =>
            {
                _e.Cancel = true;
                var selectPage = PageListView.SelectedItem as CustomPage;
                if (data != null)
                {
                    if (selectPage != null && selectPage.Id == data.Id)
                    {
                        MainFrame.Navigate(typeof(Pages.WelcomePage));
                    }
                    confirmDialog.IsPrimaryButtonEnabled = false;
                    confirmDialog.PrimaryButtonText = AppTools.GetReswLanguage("Tip_Waiting");
                    await IOTools.DeletePage(data);
                    CustomPages.Remove(data);
                    _pageListCount -= 1;
                    new PopupToast(AppTools.GetReswLanguage("Tip_DeletePageSuccess")).ShowPopup();
                    confirmDialog.Hide();
                }
                else
                {
                    new PopupToast(AppTools.GetReswLanguage("Tip_NoPageSelected"), AppTools.GetThemeSolidColorBrush(ColorType.ErrorColor)).ShowPopup();
                }
            };
            await confirmDialog.ShowAsync();
        }

        private void HomePageMenuItem_Click(object sender, RoutedEventArgs e)
        {
            var data = (sender as FrameworkElement).DataContext as CustomPage;
            AppTools.WriteLocalSetting(AppSettings.ScreenPage, data.Id);
            AppTools.WriteLocalSetting(AppSettings.IsScreenChannelCustom, "False");
            AppTools.WriteLocalSetting(AppSettings.IsScreenPageCustom, "True");
            new PopupToast(AppTools.GetReswLanguage("Tip_Saved")).ShowPopup();
        }

        private async void MoveChannelMenuItem_Click(object sender, RoutedEventArgs e)
        {
            var data = (sender as FrameworkElement).DataContext as Channel;
            var dialog = new MoveChannelDialog(data);
            await dialog.ShowAsync();
        }

        private async void UpdateCategoryMenuItem_Click(object sender, RoutedEventArgs e)
        {
            var data = (sender as FrameworkElement).DataContext as Category;
            var dialog = new ModifyCategoryDialog(data);
            await dialog.ShowAsync();
        }

        private async void DeleteCategoryMenuItem_Click(object sender, RoutedEventArgs e)
        {
            var data = (sender as FrameworkElement).DataContext as Category;
            var confirmDialog = new ConfirmDialog(AppTools.GetReswLanguage("Tip_DeleteWarning"), AppTools.GetReswLanguage("Tip_DeleteCategoryWarning"));
            confirmDialog.PrimaryButtonClick += async (_s, _e) =>
            {
                _e.Cancel = true;
                var selectCategory = CategoryListView.SelectedItem as Category;
                if (data != null)
                {
                    if (selectCategory != null && selectCategory.Id == data.Id)
                    {
                        Channels.Clear();
                        _channelListCount = 0;
                        SideChannelGrid.Visibility = Visibility.Collapsed;
                        MainFrame.Navigate(typeof(Pages.WelcomePage));
                    }
                    confirmDialog.IsPrimaryButtonEnabled = false;
                    confirmDialog.PrimaryButtonText = AppTools.GetReswLanguage("Tip_Waiting");
                    await IOTools.DeleteCategory(data);
                    Categories.Remove(data);
                    _categoryListCount -= 1;
                    new PopupToast(AppTools.GetReswLanguage("Tip_DeleteCategorySuccess")).ShowPopup();
                    data = null;
                    confirmDialog.Hide();
                }
                else
                {
                    new PopupToast(AppTools.GetReswLanguage("Tip_NoCategorySelected"), AppTools.GetThemeSolidColorBrush(ColorType.ErrorColor)).ShowPopup();
                }
            };
            await confirmDialog.ShowAsync();
        }

        private void Page_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            double width = e.NewSize.Width;
            if (width > 1400)
            {
                AppSplitView.IsPaneOpen = true;
                AppSplitView.DisplayMode = SplitViewDisplayMode.Inline;
                MinsizeHeaderContainer.Visibility = Visibility.Collapsed;
            }
            else
            {
                AppSplitView.IsPaneOpen = false;
                AppSplitView.DisplayMode = SplitViewDisplayMode.Overlay;
                MinsizeHeaderContainer.Visibility = Visibility.Visible;
            }
        }

        private void MenuButton_Click(object sender, RoutedEventArgs e)
        {
            AppSplitView.IsPaneOpen = !AppSplitView.IsPaneOpen;
        }

        private void TodoButton_Click(object sender, RoutedEventArgs e)
        {
            _isTodoButtonClick = true;
            if (MinsizeHeaderContainer.Visibility == Visibility.Visible)
            {
                AppSplitView.IsPaneOpen = false;
            }
            SettingButton.IsChecked = false;
            StarButton.IsChecked = false;
            PageListView.SelectedIndex = -1;
            SideChannelGrid.Visibility = Visibility.Collapsed;
            AppSplitView.OpenPaneLength = 250;
            CategoryListView.SelectedIndex = -1;
            string title = AppTools.GetReswLanguage("Tip_TodoList");
            if (MainFrame.Content is Pages.FeedCollectionPage)
            {
                Pages.FeedCollectionPage.Current.UpdateLayout(TodoList, title);
            }
            else
            {
                MainFrame.Navigate(typeof(Pages.FeedCollectionPage), new Tuple<List<RssSchema>, string>(TodoList, title));
            }
            TodoButton.IsChecked = true;
        }
        private void StarButton_Click(object sender, RoutedEventArgs e)
        {
            _isTodoButtonClick = false;
            if (MinsizeHeaderContainer.Visibility == Visibility.Visible)
            {
                AppSplitView.IsPaneOpen = false;
            }
            TodoButton.IsChecked = false;
            SettingButton.IsChecked = false;
            PageListView.SelectedIndex = -1;
            SideChannelGrid.Visibility = Visibility.Collapsed;
            AppSplitView.OpenPaneLength = 250;
            CategoryListView.SelectedIndex = -1;
            string title = AppTools.GetReswLanguage("Tip_StarList");
            if (MainFrame.Content is Pages.FeedCollectionPage)
            {
                Pages.FeedCollectionPage.Current.UpdateLayout(StarList, title);
            }
            else
            {
                MainFrame.Navigate(typeof(Pages.FeedCollectionPage), new Tuple<List<RssSchema>, string>(StarList, title));
            }
            StarButton.IsChecked = true;
        }

        private async void ToastChannelMenuItem_Click(object sender, RoutedEventArgs e)
        {
            var data = (sender as FrameworkElement).DataContext as Channel;
            if (ToastList.Any(p => p.Link == data.Link || p.Id==data.Id))
            {
                new PopupToast(AppTools.GetReswLanguage("Tip_ToastRepeat"), AppTools.GetThemeSolidColorBrush(ColorType.ErrorColor)).ShowPopup();
            }
            else
            {
                await IOTools.AddNeedToastChannel(data);
                ToastList.Add(data);
                new PopupToast(AppTools.GetReswLanguage("Tip_Added")).ShowPopup();
            }
        }
        /// <summary>
        /// 在应用启动时导航到指定频道
        /// </summary>
        public void SelectChannelByCustom()
        {
            bool IsCustomHome = Convert.ToBoolean(AppTools.GetLocalSetting(AppSettings.IsScreenChannelCustom, "False"));
            bool IsCustomPage = Convert.ToBoolean(AppTools.GetLocalSetting(AppSettings.IsScreenPageCustom, "False"));
            string channelId = AppTools.GetLocalSetting(AppSettings.ScreenChannel, "");
            string pageId = AppTools.GetLocalSetting(AppSettings.ScreenPage, "");
            if (IsCustomHome)
            {
                SideChannelGrid.Visibility = Visibility.Visible;
                AppSplitView.OpenPaneLength = 550;
                if (string.IsNullOrEmpty(channelId))
                {
                    CategoryListView.SelectedItem = Categories.FirstOrDefault();
                    CategoryNameTextBlock.Text = Categories.First().Name;
                    foreach (var channel in Categories.First().Channels)
                    {
                        Channels.Add(channel);
                    }
                    ChannelListView.SelectedItem = Channels.FirstOrDefault();
                    if (Channels.Count > 0)
                    {
                        MainFrame.Navigate(typeof(Pages.ChannelDetailPage), Channels.First());
                    }
                    else
                    {
                        MainFrame.Navigate(typeof(Pages.WelcomePage));
                    }
                }
                else
                {
                    Channel selectItem=null;
                    foreach (var cat in Categories)
                    {
                        if (cat.Channels.Any(p => p.Id == channelId))
                        {
                            CategoryListView.SelectedItem = cat;
                            CategoryNameTextBlock.Text = cat.Name;
                            foreach (var cha in cat.Channels)
                            {
                                Channels.Add(cha);
                            }
                            selectItem = Channels.Where(c => c.Id == channelId).FirstOrDefault();
                        }
                    }
                    if (selectItem != null)
                    {
                        ChannelListView.SelectedItem = selectItem;
                        MainFrame.Navigate(typeof(Pages.ChannelDetailPage), selectItem);
                    }
                    else
                    {
                        CategoryListView.SelectedItem = Categories.FirstOrDefault();
                        CategoryNameTextBlock.Text = Categories.First().Name;
                        MainFrame.Navigate(typeof(Pages.WelcomePage));
                        foreach (var channel in Categories.First().Channels)
                        {
                            Channels.Add(channel);
                        }
                    }
                }
            }
            else if (IsCustomPage)
            {
                CategoryNameTextBlock.Text = "RSS Stalker";
                SideChannelGrid.Visibility = Visibility.Collapsed;
                AppSplitView.OpenPaneLength = 250;
                if (string.IsNullOrEmpty(pageId))
                {
                    var first= CustomPages.FirstOrDefault();
                    if (first != null)
                    {
                        PageListView.SelectedItem = first;
                        CategoryNameTextBlock.Text = first.Name;
                        MainFrame.Navigate(typeof(Pages.CustomPageDetailPage), first);
                    }
                    else
                    {
                        MainFrame.Navigate(typeof(Pages.WelcomePage));
                    }
                }
                else
                {
                    CustomPage selectItem = null;
                    foreach (var page in CustomPages)
                    {
                        if (page.Id == pageId)
                        {
                            PageListView.SelectedItem = page;
                            CategoryNameTextBlock.Text = page.Name;
                            selectItem = page;
                        }
                    }
                    if (selectItem != null)
                    {
                        MainFrame.Navigate(typeof(Pages.CustomPageDetailPage), selectItem);
                    }
                    else
                    {
                        MainFrame.Navigate(typeof(Pages.WelcomePage));
                    }
                }
            }
            else
            {
                SideChannelGrid.Visibility = Visibility.Visible;
                AppSplitView.OpenPaneLength = 550;
                CategoryListView.SelectedItem = Categories.FirstOrDefault();
                CategoryNameTextBlock.Text = Categories.First().Name;
                foreach (var channel in Categories.First().Channels)
                {
                    Channels.Add(channel);
                }
                MainFrame.Navigate(typeof(Pages.WelcomePage));
            }
            
        }

        private void HomeChannelMenuItem_Click(object sender, RoutedEventArgs e)
        {
            var data = (sender as FrameworkElement).DataContext as Channel;
            AppTools.WriteLocalSetting(AppSettings.ScreenChannel, data.Id);
            AppTools.WriteLocalSetting(AppSettings.IsScreenChannelCustom, "True");
            AppTools.WriteLocalSetting(AppSettings.IsScreenPageCustom, "False");
            new PopupToast(AppTools.GetReswLanguage("Tip_Saved")).ShowPopup();
        }

        private async void CacheChannelMenuItem_Click(object sender, RoutedEventArgs e)
        {
            var data = (sender as FrameworkElement).DataContext as Channel;
            LoadingRing.IsActive = true;
            try
            {
                await IOTools.AddCacheChannel(null,data);
                new PopupToast(AppTools.GetReswLanguage("Tip_CacheSuccess")).ShowPopup();
            }
            catch (Exception)
            {
                new PopupToast(AppTools.GetReswLanguage("Tip_CacheFailed"),AppTools.GetThemeSolidColorBrush(ColorType.ErrorColor)).ShowPopup();
            }
            LoadingRing.IsActive = false;
        }

        private async void CacheCategoryMenuItem_Click(object sender, RoutedEventArgs e)
        {
            var data = (sender as FrameworkElement).DataContext as Category;
            LoadingRing.IsActive = true;
            CacheProgressBar.Visibility = Visibility.Visible;
            try
            {
                CacheProgressBar.Maximum = data.Channels.Count;
                await IOTools.AddCacheChannel(async (count)=> {
                    await DispatcherHelper.ExecuteOnUIThreadAsync(() =>
                    {
                        CacheProgressBar.Value = count;
                    });
                     },
                    data.Channels.ToArray());
                new PopupToast(AppTools.GetReswLanguage("Tip_CacheSuccess")).ShowPopup();
            }
            catch (Exception)
            {
                new PopupToast(AppTools.GetReswLanguage("Tip_CacheFailed"), AppTools.GetThemeSolidColorBrush(ColorType.ErrorColor)).ShowPopup();
            }
            CacheProgressBar.Visibility = Visibility.Collapsed;
            LoadingRing.IsActive = false;
        }

        private void AddPageButton_Click(object sender, RoutedEventArgs e)
        {
            bool isEmpty = true;
            foreach (var item in Categories)
            {
                if (item.Channels.Count > 0)
                {
                    isEmpty = false;
                    break;
                }
            }
            if (!isEmpty)
            {
                SettingButton.IsChecked = false;
                StarButton.IsChecked = false;
                PageListView.SelectedIndex = -1;
                SettingButton.IsChecked = false;
                CategoryListView.SelectedIndex = -1;
                SideChannelGrid.Visibility = Visibility.Collapsed;
                AppSplitView.OpenPaneLength = 250;
                CategoryNameTextBlock.Text = AppTools.GetReswLanguage("Tip_AddCustomPage");
                MainFrame.Navigate(typeof(Pages.OperaterCustomPage));
            }
            else
                new PopupToast(AppTools.GetReswLanguage("Tip_AddChannelFirst"),AppTools.GetThemeSolidColorBrush(ColorType.ErrorColor)).ShowPopup();
        }

        private async void PageListView_ItemClick(object sender, ItemClickEventArgs e)
        {
            var item = e.ClickedItem as CustomPage;
            CloseCategory();
            if (MinsizeHeaderContainer.Visibility == Visibility.Visible)
            {
                AppSplitView.IsPaneOpen = false;
            }
            CategoryNameTextBlock.Text = item.Name;
            if (MainFrame.Content is Pages.CustomPageDetailPage)
            {
                await Pages.CustomPageDetailPage.Current.UpdateLayout(item);
            }
            else
            {
                MainFrame.Navigate(typeof(Pages.CustomPageDetailPage), item);
            }
        }
        public void CloseCategory()
        {
            TodoButton.IsChecked = false;
            StarButton.IsChecked = false;
            SettingButton.IsChecked = false;
            CategoryListView.SelectedIndex = -1;
            ChannelListView.SelectedIndex = -1;
            SideChannelGrid.Visibility = Visibility.Collapsed;
            AppSplitView.OpenPaneLength = 250;
            _channelListCount = -1;
            Channels.Clear();
            _channelListCount = 0;
        }
        public async void AddReadId(params string[] ids)
        {
            bool isAdd = false;
            int addCount = 0;
            foreach (var id in ids)
            {
                if (!ReadIds.Contains(id))
                {
                    ReadIds.Add(id);
                    addCount += 1;
                    isAdd = true;
                }
            }
            if (isAdd)
            {
                try
                {
                    CategroyAndChannelDecrease(addCount);
                    await IOTools.ReplaceReadIds(ReadIds);
                }
                catch (Exception)
                {

                }
            }
        }

        private async void ReadableChannelMenuItem_Click(object sender, RoutedEventArgs e)
        {
            var data = (sender as FrameworkElement).DataContext as Channel;
            if (ReadableList.Any(p => p.Link == data.Link || p.Id == data.Id))
            {
                new PopupToast(AppTools.GetReswLanguage("Tip_ReadableRepeat"), AppTools.GetThemeSolidColorBrush(ColorType.ErrorColor)).ShowPopup();
            }
            else
            {
                await IOTools.AddNeedReadableChannel(data);
                ReadableList.Add(data);
                new PopupToast(AppTools.GetReswLanguage("Tip_Added")).ShowPopup();
            }
        }
        private async Task CacheAll()
        {
            if (!NetworkHelper.Instance.ConnectionInformation.IsInternetAvailable)
            {
                return;
            }
            var list = new List<Channel>();
            foreach (var item in Categories)
            {
                foreach (var cha in item.Channels)
                {
                    list.Add(cha);
                }
            }
            
            if (list.Count > 0)
            {
                try
                {
                    CacheProgressBar.Visibility = Visibility.Visible;
                    CacheProgressBar.Maximum = list.Count;
                    int channelCount = 0;
                    var categoryTask = new List<Task>();
                    TempCache.Clear();
                    foreach (var category in Categories)
                    {
                        category.NoRead = 0;
                        var channelTasks = new List<Task>();
                        int cateNoRead = 0;
                        foreach (var channel in category.Channels)
                        {
                            channel.NoRead = 0;
                            channelTasks.Add(Task.Run(async () =>
                            {
                                await DispatcherHelper.ExecuteOnUIThreadAsync(async () =>
                                {
                                    var feeds = await AppTools.GetFeedsFromUrl(channel.Link, true, (d) =>
                                    {
                                        var noread = d.Where(p => !ReadIds.Any(u => u == p.InternalID));
                                        channel.NoRead = noread.Count();
                                        cateNoRead += noread.Count();
                                        if (Channels.Count != 0)
                                        {
                                            var showChannel = Channels.Where(p => p.Id == channel.Id).FirstOrDefault();
                                            if (showChannel != null)
                                            {
                                                showChannel.NoRead = channel.NoRead;
                                            }
                                        }
                                        TempCache.Add(new CacheModel()
                                        {
                                            Channel = channel,
                                            Feeds = d,
                                            CacheTime = AppTools.DateToTimeStamp(DateTime.Now.ToLocalTime())
                                        });
                                    });
                                    channelCount += 1;
                                    CacheProgressBar.Value = channelCount;
                                    
                                });
                            }));
                        }
                        categoryTask.Add(Task.Run(async () =>
                        {
                            await DispatcherHelper.ExecuteOnUIThreadAsync(async () =>
                            {
                                await Task.WhenAll(channelTasks);
                                category.NoRead = cateNoRead;
                                
                            });
                        }));
                    }
                    await Task.WhenAll(categoryTask);
                    await IOTools.AddCacheChannel(TempCache);
                    CacheProgressBar.Visibility = Visibility.Collapsed;
                    TempCache.Clear();
                }
                catch (Exception ex)
                {
                    new PopupToast(ex.Message).ShowPopup();
                }
            }
        }
        public void CategroyAndChannelDecrease(int count=1)
        {
            var cate = CategoryListView.SelectedItem as Category;
            var channel = ChannelListView.SelectedItem as Channel;
            if(cate!=null && channel != null)
            {
                if (cate.NoRead > count-1)
                    cate.NoRead -= count;
                if (channel.NoRead > count-1)
                    channel.NoRead -= count;
            }
        }
    }
}
