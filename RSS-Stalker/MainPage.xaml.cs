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
        public MainPage()
        {
            this.InitializeComponent();
            Current = this;
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

        private void ChannelListView_ItemClick(object sender, ItemClickEventArgs e)
        {
            var item = e.ClickedItem as Channel;
            MainFrame.Navigate(typeof(Pages.ChannelDetailPage), item);
        }

        private async void AddCategoryButton_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new AddCategoryDialog();
            await dialog.ShowAsync();
        }

        private void ChannelSearchBox_TextChanged(object sender, TextChangedEventArgs e)
        {

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

        private void UpdateChannelMenuItem_Click(object sender, RoutedEventArgs e)
        {

        }

        private async void DeleteChannelMenuItem_Click(object sender, RoutedEventArgs e)
        {
            var confirmDialog = new ConfirmDialog(AppTools.GetReswLanguage("Tip_DeleteWarning"), AppTools.GetReswLanguage("Tip_DeleteChannelWarning"));
            confirmDialog.PrimaryButtonClick += async (_s, _e) =>
            {
                _e.Cancel = true;
                var category = CategoryListView.SelectedItem as Category;
                if (category != null)
                {
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

        private void MoveChannelMenuItem_Click(object sender, RoutedEventArgs e)
        {

        }
    }
}
