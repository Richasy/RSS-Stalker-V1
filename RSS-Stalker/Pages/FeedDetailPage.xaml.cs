using RSS_Stalker.Controls;
using RSS_Stalker.Models;
using RSS_Stalker.Tools;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.ApplicationModel.DataTransfer;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Storage;
using Windows.System;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// https://go.microsoft.com/fwlink/?LinkId=234238 上介绍了“空白页”项模板

namespace RSS_Stalker.Pages
{
    /// <summary>
    /// 可用于自身或导航至 Frame 内部的空白页。
    /// </summary>
    public sealed partial class FeedDetailPage : Page
    {
        private Feed _sourceFeed;
        private ObservableCollection<Feed> ShowFeeds = new ObservableCollection<Feed>();
        private List<Feed> AllFeeds = new List<Feed>();
        private bool _isInit = false;
        public FeedDetailPage()
        {
            this.InitializeComponent();
            ToolTipService.SetToolTip(AddTodoButton, AppTools.GetReswLanguage("Tip_AddTodoList"));
            ToolTipService.SetToolTip(RemoveTodoButton, AppTools.GetReswLanguage("Tip_DeleteTodoList"));
            ToolTipService.SetToolTip(AddStarButton, AppTools.GetReswLanguage("Tip_AddStarList"));
            ToolTipService.SetToolTip(RemoveStarButton, AppTools.GetReswLanguage("Tip_RemoveStarList"));
        }
        protected override async void OnNavigatedTo(NavigationEventArgs e)
        {
            if(e.Parameter!=null && e.Parameter is Tuple<Feed,List<Feed>>)
            {
                var data = e.Parameter as Tuple<Feed, List<Feed>>;
                
                _sourceFeed = data.Item1;
                AllFeeds = data.Item2;
                if (MainPage.Current.TodoList.Any(p => p.Equals(_sourceFeed)))
                {
                    AddTodoButton.Visibility = Visibility.Collapsed;
                    RemoveTodoButton.Visibility = Visibility.Visible;
                }
                else
                {
                    AddTodoButton.Visibility = Visibility.Visible;
                    RemoveTodoButton.Visibility = Visibility.Collapsed;
                }
                if (MainPage.Current.StarList.Any(p => p.Equals(_sourceFeed)))
                {
                    AddStarButton.Visibility = Visibility.Collapsed;
                    RemoveStarButton.Visibility = Visibility.Visible;
                }
                else
                {
                    AddStarButton.Visibility = Visibility.Visible;
                    RemoveStarButton.Visibility = Visibility.Collapsed;
                }
                foreach (var item in data.Item2)
                {
                    if (item.InternalID != _sourceFeed.InternalID)
                    {
                        ShowFeeds.Add(item);
                    }
                }
                TitleTextBlock.Text = _sourceFeed.Title;
                LoadingRing.IsActive = true;
                string theme = AppTools.GetRoamingSetting(Enums.AppSettings.Theme, "Light");
                string css = await FileIO.ReadTextAsync(await StorageFile.GetFileFromApplicationUriAsync(new Uri($"ms-appx:///Template/{theme}.css")));
                string html = AppTools.GetHTML(css, _sourceFeed.Content ?? "");
                DetailWebView.NavigateToString(html);
                _isInit = true;
            }
        }

        private void DetailWebView_Loaded(object sender, RoutedEventArgs e)
        {
            LoadingRing.IsActive = false;
        }

        private async void FeedListView_ItemClick(object sender, ItemClickEventArgs e)
        {
            var data = e.ClickedItem as Feed;
            _sourceFeed = data;
            ShowFeeds.Clear();
            foreach (var item in AllFeeds)
            {
                if (item.InternalID != data.InternalID)
                {
                    ShowFeeds.Add(item);
                }
            }
            TitleTextBlock.Text = _sourceFeed.Title;
            string theme = AppTools.GetRoamingSetting(Enums.AppSettings.Theme, "Light");
            string css = await FileIO.ReadTextAsync(await StorageFile.GetFileFromApplicationUriAsync(new Uri($"ms-appx:///Template/{theme}.css")));
            string html = AppTools.GetHTML(css, _sourceFeed.Content ?? "");
            DetailWebView.NavigateToString(html);
        }

        private void GridViewButton_Click(object sender, RoutedEventArgs e)
        {
            MainPage.Current.MainFrame.Navigate(typeof(ChannelDetailPage), AllFeeds);
        }

        private void MoreButton_Click(object sender, RoutedEventArgs e)
        {
            FlyoutBase.ShowAttachedFlyout(sender as FrameworkElement);
        }

        private async void Menu_Web_Click(object sender, RoutedEventArgs e)
        {
            await Launcher.LaunchUriAsync(new Uri(_sourceFeed.FeedUrl));
        }

        private void Menu_Share_Click(object sender, RoutedEventArgs e)
        {
            DataTransferManager dataTransferManager = DataTransferManager.GetForCurrentView();
            dataTransferManager.DataRequested += IndexPage_DataRequested;
            DataTransferManager.ShowShareUI();
        }
        private void IndexPage_DataRequested(DataTransferManager sender, DataRequestedEventArgs args)
        {
            //创建一个数据包
            DataPackage dataPackage = new DataPackage();

            //把要分享的链接放到数据包里
            dataPackage.SetHtmlFormat(HtmlFormatHelper.CreateHtmlFormat(_sourceFeed.Content));
            dataPackage.SetWebLink(new Uri(_sourceFeed.FeedUrl));
            //数据包的标题（内容和标题必须提供）
            dataPackage.Properties.Title = _sourceFeed.Title;
            //数据包的描述
            dataPackage.Properties.Description = _sourceFeed.Summary;
            //给dataRequest对象赋值
            DataRequest request = args.Request;
            request.Data = dataPackage;
        }

        private async void Menu_ReInit_Click(object sender, RoutedEventArgs e)
        {
            string theme = AppTools.GetRoamingSetting(Enums.AppSettings.Theme, "Light");
            string css = await FileIO.ReadTextAsync(await StorageFile.GetFileFromApplicationUriAsync(new Uri($"ms-appx:///Template/{theme}.css")));
            string html = AppTools.GetHTML(css, _sourceFeed.Content ?? "");
            DetailWebView.NavigateToString(html);
        }

        private void SideListButton_Click(object sender, RoutedEventArgs e)
        {
            DetailSplitView.IsPaneOpen = !DetailSplitView.IsPaneOpen;
        }

        private void Page_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            double width = e.NewSize.Width;
            if (!_isInit)
            {
                if(DetailSplitView!=null)
                    DetailSplitView.IsPaneOpen = width >= 900 ? true : false;
            }
        }

        private async void AddTodoButton_Click(object sender, RoutedEventArgs e)
        {
            (sender as Button).IsEnabled = false;
            try
            {
                await IOTools.AddTodoRead(_sourceFeed);
                MainPage.Current.TodoList.Add(_sourceFeed);
                AddTodoButton.Visibility = Visibility.Collapsed;
                RemoveTodoButton.Visibility = Visibility.Visible;
                new PopupToast(AppTools.GetReswLanguage("Tip_AddTodoListSuccess")).ShowPopup();
            }
            catch (Exception ex)
            {
                new PopupToast(ex.Message).ShowPopup();
            }
            (sender as Button).IsEnabled = true;
        }

        private async void AddStarButton_Click(object sender, RoutedEventArgs e)
        {
            (sender as Button).IsEnabled = false;
            try
            {
                await IOTools.AddStar(_sourceFeed);
                MainPage.Current.StarList.Add(_sourceFeed);
                AddStarButton.Visibility = Visibility.Collapsed;
                RemoveStarButton.Visibility = Visibility.Visible;
                new PopupToast(AppTools.GetReswLanguage("Tip_AddStarListSuccess")).ShowPopup();
            }
            catch (Exception ex)
            {
                new PopupToast(ex.Message).ShowPopup();
            }
            (sender as Button).IsEnabled = true;
        }

        private async void RemoveTodoButton_Click(object sender, RoutedEventArgs e)
        {
            (sender as Button).IsEnabled = false;
            try
            {
                await IOTools.DeleteTodoRead(_sourceFeed);
                MainPage.Current.TodoList.RemoveAll(p=>p.Equals(_sourceFeed));
                AddTodoButton.Visibility = Visibility.Visible;
                RemoveTodoButton.Visibility = Visibility.Collapsed;
                new PopupToast(AppTools.GetReswLanguage("Tip_DeleteTodoListSuccess")).ShowPopup();
            }
            catch (Exception ex)
            {
                new PopupToast(ex.Message).ShowPopup();
            }
            (sender as Button).IsEnabled = true;
        }

        private async void RemoveStarButton_Click(object sender, RoutedEventArgs e)
        {
            (sender as Button).IsEnabled = false;
            try
            {
                await IOTools.DeleteStar(_sourceFeed);
                MainPage.Current.StarList.RemoveAll(p => p.Equals(_sourceFeed));
                AddStarButton.Visibility = Visibility.Visible;
                RemoveStarButton.Visibility = Visibility.Collapsed;
                new PopupToast(AppTools.GetReswLanguage("Tip_DeleteStarListSuccess")).ShowPopup();
            }
            catch (Exception ex)
            {
                new PopupToast(ex.Message).ShowPopup();
            }
            (sender as Button).IsEnabled = true;
        }
    }
}
