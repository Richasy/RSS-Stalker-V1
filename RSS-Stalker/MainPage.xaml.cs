using RSS_Stalker.Controls;
using RSS_Stalker.Dialog;
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
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

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
        public static MainPage Current;
        private Channel _tempChannel;
        private Category _tempCategory;
        private DispatcherTimer _checkUpdateTimer=new DispatcherTimer();
        public MainPage()
        {
            this.InitializeComponent();
            Current = this;
            Window.Current.SetTitleBar(TitleBarControl);
            PageInit();
            
        }

        private async void PageInit()
        {
            AppTools.SetTitleBarColor();
            var categories = await IOTools.GetLocalCategories();
            Channels.Clear();
            Categories.Clear();
            foreach (var item in categories)
            {
                Categories.Add(item);
            }
            if (Categories.Count == 0)
            {
                SideChannelGrid.Visibility = Visibility.Collapsed;
            }
            else
            {
                CategoryListView.SelectedItem = Categories.First();
                foreach (var channel in Categories.First().Channels)
                {
                    Channels.Add(channel);
                }
            }
            MainFrame.Navigate(typeof(Pages.WelcomePage));
            await App.OneDrive.OneDriveAuthorize();
            TimerInit();
        }

        private void TimerInit()
        {
            _checkUpdateTimer.Interval = new TimeSpan(0, 1, 0);
            _checkUpdateTimer.Tick += CheckRssListUpdate;
            _checkUpdateTimer.Start();
        }

        private async void CheckRssListUpdate(object sender, object e)
        {
            string localTime = AppTools.GetLocalSetting(Enums.AppSettings.UpdateTime, "0");
            string roamTime = AppTools.GetRoamingSetting(Enums.AppSettings.UpdateTime, "0");
            if (localTime != roamTime)
            {
                var list = await App.OneDrive.GetCategoryList();
                await IOTools.ReplaceCategory(list);
                string selectCategoryId = (CategoryListView.SelectedItem as Category)?.Id;
                string selectChannelLink = (ChannelListView.SelectedItem as Channel)?.Link;
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
                            if (!string.IsNullOrEmpty(selectChannelLink))
                            {
                                var selectChannel = Channels.Where(c => c.Link == selectChannelLink).FirstOrDefault();
                                if (selectChannel != null)
                                    ChannelListView.SelectedItem = selectChannel;
                            }
                        }
                    }
                }
                AppTools.WriteLocalSetting(Enums.AppSettings.UpdateTime, roamTime);
                new PopupToast(AppTools.GetReswLanguage("Tip_Updated")).ShowPopup();
            }
        }

        private void CategoryListView_ItemClick(object sender, ItemClickEventArgs e)
        {
            var item = e.ClickedItem as Category;
            ChannelListView.SelectedIndex = -1;
            SideChannelGrid.Visibility = Visibility.Visible;
            Channels.Clear();
            foreach (var cha in item.Channels)
            {
                Channels.Add(cha);
            }
            if(!(MainFrame.Content is Pages.WelcomePage))
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
                    Channels.Clear();
                    foreach (var item in list)
                    {
                        Channels.Add(item);
                    }
                }
            }
            else
            {
                Channels.Clear();
                if (selectCategory != null)
                {
                    foreach (var item in selectCategory.Channels)
                    {
                        Channels.Add(item);
                    }
                }
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
                    if(selectChannel!=null && selectChannel.Link == _tempChannel.Link)
                    {
                        MainFrame.Navigate(typeof(Pages.WelcomePage));
                    }
                    confirmDialog.IsPrimaryButtonEnabled = false;
                    confirmDialog.PrimaryButtonText = AppTools.GetReswLanguage("Tip_Waiting");
                    category.Channels.RemoveAll(p => p.Link == _tempChannel.Link);
                    await IOTools.UpdateCategory(category);
                    Channels.Remove(_tempChannel);
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
                        SideChannelGrid.Visibility = Visibility.Collapsed;
                        MainFrame.Navigate(typeof(Pages.WelcomePage));
                    }
                    confirmDialog.IsPrimaryButtonEnabled = false;
                    confirmDialog.PrimaryButtonText = AppTools.GetReswLanguage("Tip_Waiting");
                    await IOTools.DeleteCategory(_tempCategory);
                    Categories.Remove(_tempCategory);
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
    }
}
