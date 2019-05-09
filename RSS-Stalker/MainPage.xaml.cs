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
                CategoryListView.SelectedItem = Categories.First();
                CategoryNameTextBlock.Text = Categories.First().Name;
                foreach (var channel in Categories.First().Channels)
                {
                    Channels.Add(channel);
                }
            }
            _categoryListCount = Categories.Count;
            _channelListCount = Channels.Count;
            MainFrame.Navigate(typeof(Pages.WelcomePage));
            await App.OneDrive.OneDriveAuthorize();
            TimerInit();
            TodoList = await IOTools.GetLocalTodoReadList();
            StarList = await IOTools.GetLocalStarList();
            ToastList = await IOTools.GetNeedToastChannels();
            RegisterBackground();
            _isInit = true;
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
            _checkUpdateTimer.Interval = new TimeSpan(0, 0, 10);
            _checkUpdateTimer.Tick += CheckRssListUpdate;
            _checkUpdateTimer.Start();
        }

        private async void CheckRssListUpdate(object sender, object e)
        {
            string localBasicTime = AppTools.GetLocalSetting(AppSettings.BasicUpdateTime, "0");
            string roamBasicTime = AppTools.GetRoamingSetting(AppSettings.BasicUpdateTime, "1");
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
            string roamTime = AppTools.GetRoamingSetting(AppSettings.BasicUpdateTime, "0");
            string selectCategoryId = (CategoryListView.SelectedItem as Category)?.Id;
            string selectChannelId = (ChannelListView.SelectedItem as Channel)?.Id;
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

        private void CategoryListView_ItemClick(object sender, ItemClickEventArgs e)
        {
            var item = e.ClickedItem as Category;
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
            AppSplitView.OpenPaneLength = 250;
            CategoryListView.SelectedIndex = -1;
            MainFrame.Navigate(typeof(Pages.SettingPage));
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
                    new PopupToast(AppTools.GetReswLanguage("Tip_NoCategorySelected")).ShowPopup();
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
                    new PopupToast(AppTools.GetReswLanguage("Tip_NoCategorySelected")).ShowPopup();
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
            if (width > 1100)
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
        }

        private void StarButton_Click(object sender, RoutedEventArgs e)
        {
            _isTodoButtonClick = false;
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
        }

        private async void ToastChannelMenuItem_Click(object sender, RoutedEventArgs e)
        {
            if (ToastList.Any(p => p.Id == _tempChannel.Id))
            {
                new PopupToast(AppTools.GetReswLanguage("Tip_ToastRepeat")).ShowPopup();
            }
            else
            {
                await IOTools.AddNeedToastChannel(_tempChannel);
                ToastList.Add(_tempChannel);
                new PopupToast(AppTools.GetReswLanguage("Tip_Added")).ShowPopup();
            }
        }
    }
}
