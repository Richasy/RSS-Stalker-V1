using RSS_Stalker.Controls;
using RSS_Stalker.Dialog;
using CoreLib.Models;
using CoreLib.Tools;
using CoreLib.Enums;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
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
using RSS_Stalker.Tools;
using Windows.ApplicationModel.Background;
using Microsoft.Toolkit.Uwp.Helpers;
using Windows.UI.Core;
using System.Threading.Tasks;
using Windows.Storage;
using Windows.ApplicationModel;

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
        public List<Feed> TodoList = new List<Feed>();
        public List<Feed> StarList = new List<Feed>();
        public List<Channel> ToastList = new List<Channel>();
        public static MainPage Current;
        public int _categoryListCount = -1;
        public int _channelListCount = -1;
        private Channel _tempChannel;
        private Category _tempCategory;
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

        private async void PageInit()
        {
            AppTools.SetTitleBarColor();
            AppTitleBlock.Text = AppTools.GetReswLanguage("DisplayName");
            Categories.CollectionChanged += CategoryCollectionReordered;
            Channels.CollectionChanged += ChannelCollectionReordered;
            var categories = await IOTools.GetLocalCategories();
            _categoryListCount = -1;
            _channelListCount = -1;
            Channels.Clear();
            Categories.Clear();
            foreach (var item in categories)
            {
                Categories.Add(item);
            }
            if (Categories.Count == 0)
            {
                CategoryNameTextBlock.Text = "RSS Stalker";
                SideChannelGrid.Visibility = Visibility.Collapsed;
            }
            else
            {
                SelectChannelByCustom();
            }
            _categoryListCount = Categories.Count;
            _channelListCount = Channels.Count;
            await App.OneDrive.OneDriveAuthorize();
            TimerInit();
            TodoList = await IOTools.GetLocalTodoReadList();
            StarList = await IOTools.GetLocalStarList();
            ToastList = await IOTools.GetNeedToastChannels();
            RegisterBackground();
            await CheckVersion();
            Window.Current.Dispatcher.AcceleratorKeyActivated += AccelertorKeyActivedHandle;
            _isInit = true;
        }

        private void AccelertorKeyActivedHandle(CoreDispatcher sender, AcceleratorKeyEventArgs args)
        {
            if (args.EventType.ToString().Contains("Down"))
            {
                var esc = Window.Current.CoreWindow.GetKeyState(Windows.System.VirtualKey.Escape);
                if (esc.HasFlag(CoreVirtualKeyStates.Down))
                {
                    if (MainFrame.Content is Pages.FeedDetailPage)
                    {
                        Pages.FeedDetailPage.Current.CheckBack();
                    }
                }
            }
        }

        private void RegisterBackground()
        {
            BackgroundTaskRegistration registered = BackgroundTaskHelper.Register(typeof(StalkerToast.Toast),
                                    new TimeTrigger(15, true),
                                    false, true,
                                    new SystemCondition(SystemConditionType.InternetAvailable));
        }
        private void TimerInit()
        {
            _checkUpdateTimer.Interval = new TimeSpan(0, 0, 30);
            _checkUpdateTimer.Tick += CheckRssListUpdate;
            _checkUpdateTimer.Start();
        }

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
        protected override void OnNavigatedFrom(NavigationEventArgs e)
        {
            _checkUpdateTimer.Stop();
            _checkUpdateTimer = null;
            base.OnNavigatedFrom(e);
        }
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
            MainFrame.Navigate(typeof(Pages.SettingPage));
            SettingButton.IsChecked = true;
        }

        private async void UpdateChannelMenuItem_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new ModifyChannelDialog(_tempChannel);
            await dialog.ShowAsync();
        }

        private async void DeleteChannelMenuItem_Click(object sender, RoutedEventArgs e)
        {
            var confirmDialog = new ConfirmDialog(AppTools.GetReswLanguage("Tip_DeleteWarning"), AppTools.GetReswLanguage("Tip_DeleteChannelWarning"));
            confirmDialog.PrimaryButtonClick += async (_s, _e) =>
            {
                _e.Cancel = true;
                var category = CategoryListView.SelectedItem as Category;
                var selectChannel = ChannelListView.SelectedItem as Channel;
                if (category != null)
                {
                    if(selectChannel!=null && selectChannel.Id == _tempChannel.Id)
                    {
                        MainFrame.Navigate(typeof(Pages.WelcomePage));
                    }
                    confirmDialog.IsPrimaryButtonEnabled = false;
                    confirmDialog.PrimaryButtonText = AppTools.GetReswLanguage("Tip_Waiting");
                    category.Channels.RemoveAll(p => p.Id == _tempChannel.Id);
                    await IOTools.UpdateCategory(category);
                    Channels.Remove(_tempChannel);
                    _channelListCount -= 1;
                    new PopupToast(AppTools.GetReswLanguage("Tip_DeleteChannelSuccess")).ShowPopup();
                    _tempChannel = null;
                    confirmDialog.Hide();
                }
                else
                {
                    new PopupToast(AppTools.GetReswLanguage("Tip_NoCategorySelected"), AppTools.GetThemeSolidColorBrush("ErrorColor")).ShowPopup();
                }
            };
            await confirmDialog.ShowAsync();
        }

        private void ChannelItem_RightTapped(object sender, RightTappedRoutedEventArgs e)
        {
            var data = (sender as FrameworkElement).DataContext as Channel;
            _tempChannel = data;
            ChannelMenuFlyout.ShowAt((FrameworkElement)sender,e.GetPosition((FrameworkElement)sender));
        }

        private async void MoveChannelMenuItem_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new MoveChannelDialog(_tempChannel);
            await dialog.ShowAsync();
        }

        private void ChannelItem_Holding(object sender, HoldingRoutedEventArgs e)
        {
            var data = (sender as FrameworkElement).DataContext as Channel;
            _tempChannel = data;
            ChannelMenuFlyout.ShowAt((FrameworkElement)sender, e.GetPosition((FrameworkElement)sender));
        }

        private async void UpdateCategoryMenuItem_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new ModifyCategoryDialog(_tempCategory);
            await dialog.ShowAsync();
        }

        private async void DeleteCategoryMenuItem_Click(object sender, RoutedEventArgs e)
        {
            var confirmDialog = new ConfirmDialog(AppTools.GetReswLanguage("Tip_DeleteWarning"), AppTools.GetReswLanguage("Tip_DeleteCategoryWarning"));
            confirmDialog.PrimaryButtonClick += async (_s, _e) =>
            {
                _e.Cancel = true;
                var selectCategory = CategoryListView.SelectedItem as Category;
                if (_tempCategory != null)
                {
                    if (selectCategory != null && selectCategory.Id == _tempCategory.Id)
                    {
                        Channels.Clear();
                        _channelListCount = 0;
                        SideChannelGrid.Visibility = Visibility.Collapsed;
                        MainFrame.Navigate(typeof(Pages.WelcomePage));
                    }
                    confirmDialog.IsPrimaryButtonEnabled = false;
                    confirmDialog.PrimaryButtonText = AppTools.GetReswLanguage("Tip_Waiting");
                    await IOTools.DeleteCategory(_tempCategory);
                    Categories.Remove(_tempCategory);
                    _categoryListCount -= 1;
                    new PopupToast(AppTools.GetReswLanguage("Tip_DeleteCategorySuccess")).ShowPopup();
                    _tempCategory = null;
                    confirmDialog.Hide();
                }
                else
                {
                    new PopupToast(AppTools.GetReswLanguage("Tip_NoCategorySelected"), AppTools.GetThemeSolidColorBrush("ErrorColor")).ShowPopup();
                }
            };
            await confirmDialog.ShowAsync();
        }

        private void CategoryItem_RightTapped(object sender, RightTappedRoutedEventArgs e)
        {
            var data = (sender as FrameworkElement).DataContext as Category;
            _tempCategory = data;
            CategoryMenuFlyout.ShowAt((FrameworkElement)sender, e.GetPosition((FrameworkElement)sender));
        }

        private void CategoryItem_Holding(object sender, HoldingRoutedEventArgs e)
        {
            var data = (sender as FrameworkElement).DataContext as Category;
            _tempCategory = data;
            CategoryMenuFlyout.ShowAt((FrameworkElement)sender, e.GetPosition((FrameworkElement)sender));
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
                MainFrame.Navigate(typeof(Pages.FeedCollectionPage), new Tuple<List<Feed>, string>(TodoList, title));
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
                MainFrame.Navigate(typeof(Pages.FeedCollectionPage), new Tuple<List<Feed>, string>(StarList, title));
            }
            StarButton.IsChecked = true;
        }

        private async void ToastChannelMenuItem_Click(object sender, RoutedEventArgs e)
        {
            if (ToastList.Any(p => p.Link == _tempChannel.Link || p.Id==_tempChannel.Id))
            {
                new PopupToast(AppTools.GetReswLanguage("Tip_ToastRepeat"), AppTools.GetThemeSolidColorBrush("ErrorColor")).ShowPopup();
            }
            else
            {
                await IOTools.AddNeedToastChannel(_tempChannel);
                ToastList.Add(_tempChannel);
                new PopupToast(AppTools.GetReswLanguage("Tip_Added")).ShowPopup();
            }
        }
        public void SelectChannelByCustom()
        {
            bool IsCustomHome = Convert.ToBoolean(AppTools.GetLocalSetting(AppSettings.IsScreenChannelCustom, "False"));
            string channelId = AppTools.GetLocalSetting(AppSettings.ScreenChannel, "");
            if (IsCustomHome)
            {
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
            else
            {
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
            AppTools.WriteLocalSetting(AppSettings.ScreenChannel, _tempChannel.Id);
            AppTools.WriteLocalSetting(AppSettings.IsScreenChannelCustom, "True");
            new PopupToast(AppTools.GetReswLanguage("Tip_Saved")).ShowPopup();
        }
    }
}
