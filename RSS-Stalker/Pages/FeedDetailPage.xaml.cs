using RSS_Stalker.Controls;
using CoreLib.Models;
using CoreLib.Tools;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Net;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.ApplicationModel.DataTransfer;
using Windows.ApplicationModel.UserActivities;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Storage;
using Windows.System;
using Windows.UI.Shell;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using CoreLib.Enums;
using RSS_Stalker.Tools;

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
        private UserActivitySession _currentActivity;
        public FeedDetailPage()
        {
            this.InitializeComponent();
            ToolTipService.SetToolTip(AddTodoButton, AppTools.GetReswLanguage("Tip_AddTodoList"));
            ToolTipService.SetToolTip(RemoveTodoButton, AppTools.GetReswLanguage("Tip_DeleteTodoList"));
            ToolTipService.SetToolTip(AddStarButton, AppTools.GetReswLanguage("Tip_AddStarList"));
            ToolTipService.SetToolTip(RemoveStarButton, AppTools.GetReswLanguage("Tip_DeleteStarList"));
        }
        protected override async void OnNavigatedTo(NavigationEventArgs e)
        {
            if(e.Parameter!=null)
            {
                if(e.Parameter is Tuple<Feed, List<Feed>>)
                {
                    var data = e.Parameter as Tuple<Feed, List<Feed>>;
                    _sourceFeed = data.Item1;
                    AllFeeds = data.Item2;
                    
                    foreach (var item in AllFeeds)
                    {
                        if (item.InternalID != _sourceFeed.InternalID)
                        {
                            ShowFeeds.Add(item);
                        }
                    }
                    //LoadingRing.IsActive = true;
                    await GenerateActivityAsync(_sourceFeed);
                }
                if(e.Parameter is string[])
                {
                    var data = e.Parameter as string[];
                    _sourceFeed = new Feed()
                    {
                        InternalID = data[0],
                        Title = data[1],
                        Content = data[2],
                        FeedUrl = data[3],
                        ImageUrl = data[4],
                        Date=data[5],
                        Summary=data[6],
                        ImgVisibility = string.IsNullOrEmpty(data[4]) ? Visibility.Collapsed : Visibility.Visible
                    };
                    GridViewButton.Visibility = Visibility.Collapsed;
                    SideListButton.Visibility = Visibility.Collapsed;
                    DetailSplitView.IsPaneOpen = false;
                    DetailSplitView.OpenPaneLength = 0;
                }
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
                TitleTextBlock.Text = _sourceFeed.Title;
                string html = await PackageHTML(_sourceFeed.Content);
                DetailWebView.NavigateToString(html);
                _isInit = true;
            }
        }
        private async Task<string> PackageHTML(string content)
        {
            string theme = AppTools.GetRoamingSetting(AppSettings.Theme, "Light");
            string css = await FileIO.ReadTextAsync(await StorageFile.GetFileFromApplicationUriAsync(new Uri($"ms-appx:///Template/{theme}.css")));
            string html = AppTools.GetHTML(css, content ?? "");
            return html;
        }
        private async Task GenerateActivityAsync(Feed feed)
        {
            try
            {
                UserActivityChannel channel = UserActivityChannel.GetDefault();
                UserActivity userActivity = await channel.GetOrCreateUserActivityAsync(feed.InternalID);
                userActivity.VisualElements.DisplayText = feed.Title;
                userActivity.VisualElements.Content = AdaptiveCardBuilder.CreateAdaptiveCardFromJson(await AppTools.CreateAdaptiveJson(feed));
                //Populate required properties
                string url = $"richasy-rss://feed?id={feed.InternalID}&summary={WebUtility.UrlEncode(feed.Summary)}&date={WebUtility.UrlEncode(feed.Date)}&img={WebUtility.UrlEncode(feed.ImageUrl)}&url={WebUtility.UrlDecode(feed.FeedUrl)}&title={WebUtility.UrlEncode(feed.Title)}&content={WebUtility.UrlEncode(feed.Content)}";
                userActivity.ActivationUri = new Uri(url);
                await userActivity.SaveAsync(); //save the new metadata

                //Dispose of any current UserActivitySession, and create a new one.
                _currentActivity?.Dispose();
                _currentActivity = userActivity.CreateSession();
            }
            catch (Exception)
            {
                return;
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
            string html = await PackageHTML(_sourceFeed.Content);
            DetailWebView.NavigateToString(html);
            await GenerateActivityAsync(_sourceFeed);
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
            string html = await PackageHTML(_sourceFeed.Content);
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

        private async void Menu_Translate_Click(object sender, RoutedEventArgs e)
        {
            string language = (sender as MenuFlyoutItem).Name.Replace("Menu_Translate_", "");
            string appId = AppTools.GetLocalSetting(AppSettings.Translate_BaiduAppId, "");
            if (string.IsNullOrEmpty(appId))
            {
                var dialog = new Dialog.BaiduTranslateDialog();
                await dialog.ShowAsync();
            }
            appId = AppTools.GetLocalSetting(AppSettings.Translate_BaiduAppId, "");
            string appKey = AppTools.GetLocalSetting(AppSettings.Translate_BaiduKey, "");
            if (string.IsNullOrEmpty(appId) || string.IsNullOrEmpty(appKey))
            {
                return;
            }
            else
            {
                LoadingRing.IsActive = true;
                string output=await TranslateTools.Translate(_sourceFeed.Content, appId, appKey, "auto", language.ToLower());
                if (!string.IsNullOrEmpty(output))
                {
                    string html = await PackageHTML(output);
                    DetailWebView.NavigateToString(html);
                }
                else
                {
                    new PopupToast(AppTools.GetReswLanguage("Tip_TranslateFailed")).ShowPopup();
                }
                LoadingRing.IsActive = false;
            }
        }
    }
}
