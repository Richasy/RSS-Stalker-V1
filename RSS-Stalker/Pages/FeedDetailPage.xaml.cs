using RSS_Stalker.Controls;
using CoreLib.Models;
using CoreLib.Tools;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Windows.ApplicationModel.DataTransfer;
using Windows.ApplicationModel.UserActivities;
using Windows.Storage;
using Windows.System;
using Windows.UI.Shell;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Navigation;
using CoreLib.Enums;
using RSS_Stalker.Tools;
using System.Text.RegularExpressions;
using Newtonsoft.Json;
using RSS_Stalker.Dialog;
using Windows.Storage.Streams;
using Microsoft.Toolkit.Uwp.Connectivity;
using Windows.UI.Xaml.Media.Animation;
using CoreLib.Models.App;
using Rss.Parsers.Rss;

// https://go.microsoft.com/fwlink/?LinkId=234238 上介绍了“空白页”项模板

namespace RSS_Stalker.Pages
{
    /// <summary>
    /// 可用于自身或导航至 Frame 内部的空白页。
    /// </summary>
    public sealed partial class FeedDetailPage : Page
    {
        private string _sourceContent;
        private RssSchema _sourceFeed;
        private ObservableCollection<RssSchema> ShowFeeds = new ObservableCollection<RssSchema>();
        private List<RssSchema> AllFeeds = new List<RssSchema>();
        private bool _isInit = false;
        private UserActivitySession _currentActivity;
        public static FeedDetailPage Current;
        private string _selectText;
        private string _tempHtml;
        /// <summary>
        /// 文章详情页面，主体是WebView
        /// </summary>
        public FeedDetailPage()
        {
            this.InitializeComponent();
            Current = this;
            NavigationCacheMode = NavigationCacheMode.Enabled;
            ToolTipService.SetToolTip(AddTodoButton, AppTools.GetReswLanguage("Tip_AddTodoList"));
            ToolTipService.SetToolTip(RemoveTodoButton, AppTools.GetReswLanguage("Tip_DeleteTodoList"));
            ToolTipService.SetToolTip(AddStarButton, AppTools.GetReswLanguage("Tip_AddStarList"));
            ToolTipService.SetToolTip(RemoveStarButton, AppTools.GetReswLanguage("Tip_DeleteStarList"));
            ToolTipService.SetToolTip(ReadabilityButton, AppTools.GetReswLanguage("Tip_Readability"));
            bool isSideLocked = Convert.ToBoolean(AppTools.GetLocalSetting(AppSettings.SideListLocked, "False"));
            if (!isSideLocked)
            {
                LockButton.Foreground = AppTools.GetThemeSolidColorBrush(ColorType.ImportantTextColor);
            }
            else
            {
                LockButton.Foreground = AppTools.GetThemeSolidColorBrush(ColorType.PrimaryColor);
            }
        }
    protected override async void OnNavigatedTo(NavigationEventArgs e)
        {
            if (e.NavigationMode == NavigationMode.Back)
            {
                ReadabilityButton.IsEnabled = true;
                return;
            }
            if(e.Parameter!=null)
            {
                ShowFeeds.Clear();
                // 这种情况表明入口点为频道
                if(e.Parameter is Tuple<RssSchema, List<RssSchema>>)
                {
                    var anim = ConnectedAnimationService.GetForCurrentView().GetAnimation("ForwardConnectedAnimation");
                    if (anim != null)
                    {
                        anim.TryStart(TitleTextBlock);
                    }
                    LoadingRing.IsActive = true;
                    var data = e.Parameter as Tuple<RssSchema, List<RssSchema>>;
                    bool isUnread = Convert.ToBoolean(AppTools.GetLocalSetting(AppSettings.IsJustUnread, "False"));
                    bool isCollection = Convert.ToBoolean(MainPage.Current.TodoButton.IsChecked) || Convert.ToBoolean(MainPage.Current.StarButton.IsChecked);
                    
                    _sourceFeed = data.Item1;
                    AllFeeds = data.Item2;
                    TitleTextBlock.Text = _sourceFeed.Title;
                    MainPage.Current.AddReadId(_sourceFeed.InternalID);
                    foreach (var item in AllFeeds)
                    {
                        if (item.InternalID != _sourceFeed.InternalID)
                        {
                            if (isUnread && !isCollection && !MainPage.Current.ReadIds.Contains(item.InternalID))
                                ShowFeeds.Add(item);
                            else if (!isUnread || isCollection)
                                ShowFeeds.Add(item);
                        }
                    }
                    if (MainPage.Current.ChannelListView.SelectedItem != null)
                    {
                        var selectChannel = MainPage.Current.ChannelListView.SelectedItem as Channel;
                        if (MainPage.Current.ReadableList.Any(c => c.Id == selectChannel.Id))
                        {
                            SmartReader.Article article = await SmartReader.Reader.ParseArticleAsync(_sourceFeed.FeedUrl);
                            if (article.IsReadable || !string.IsNullOrEmpty(article.TextContent))
                            {
                                _sourceFeed.Content = article.Content;
                            }
                            else
                            {
                                new PopupToast(AppTools.GetReswLanguage("Tip_ReadError"), AppTools.GetThemeSolidColorBrush(ColorType.ErrorColor)).ShowPopup();
                            }
                        }
                    }
                    _sourceContent = _sourceFeed.Content;
                    await GenerateActivityAsync(_sourceFeed);
                }
                // 这种情况表明入口点是动态卡片
                else if(e.Parameter is string[])
                {
                    var data = e.Parameter as string[];
                    _sourceFeed = new RssSchema()
                    {
                        InternalID = data[0],
                        Title = data[1],
                        Content = data[2],
                        FeedUrl = data[3],
                        ImageUrl = data[4],
                        PublishDate = Convert.ToDateTime(data[5]),
                        Summary=data[6],
                    };
                    _sourceContent = data[2];
                    LoadingRing.IsActive = true;
                    GridViewButton.Visibility = Visibility.Collapsed;
                    SideListButton.Visibility = Visibility.Collapsed;
                    FeedListView.Visibility = Visibility.Collapsed;
                    Grid.SetColumn(SideControlContainer, 1);
                    MainPage.Current.SideHide();
                    SideControlContainer.HorizontalAlignment = HorizontalAlignment.Right;
                    SideControlContainer.Margin = new Thickness(0, 0, 10, 0);
                    DetailSplitView.IsPaneOpen = false;
                }
                ButtonStatusCheck();
                TitleTextBlock.Text = _sourceFeed.Title;
                string html = await PackageHTML(_sourceFeed.Content??_sourceFeed.Summary);
                DetailWebView.NavigateToString(html);
                string fontFamily = AppTools.GetLocalSetting(AppSettings.ReadFontFamily, "Tw Cen MT");
                string fontSize = AppTools.GetLocalSetting(AppSettings.ReadFontSize, "16");
                var selectFontFamily = MainPage.Current.SystemFonts.Where(p => p.Name == fontFamily).FirstOrDefault();
                FontFamilyComboBox.SelectedItem = selectFontFamily;
                FontSizeTextBox.Text = fontSize;
                _isInit = true;
            }
        }
        
        /// <summary>
        /// 朗读文本
        /// </summary>
        /// <param name="text">文本</param>
        /// <returns></returns>
        async Task SpeakTextAsync(string text)
        {
            LoadingRing.IsActive = true;
            IRandomAccessStream stream = await AppTools.SynthesizeTextToSpeechAsync(text);
            LoadingRing.IsActive = false;
            await VoiceMediaElement.PlayStreamAsync(stream, true,()=>
            {
                MediaControlButton.Visibility = Visibility.Collapsed;
            });
        }
        /// <summary>
        /// 检查TodoButton和StarButton的状态，根据不同情况切换不同按钮
        /// </summary>
        private void ButtonStatusCheck()
        {
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
        }
        /// <summary>
        /// 包装HTML页面
        /// </summary>
        /// <param name="content">文章主题</param>
        /// <returns></returns>
        private async Task<string> PackageHTML(string content)
        {
            string html = await FileIO.ReadTextAsync(await StorageFile.GetFileFromApplicationUriAsync(new Uri("ms-appx:///Template/ShowPage.html")));
            string theme = AppTools.GetRoamingSetting(AppSettings.Theme,"Light");
            string css = await FileIO.ReadTextAsync(await StorageFile.GetFileFromApplicationUriAsync(new Uri($"ms-appx:///Template/{theme}.css")));
            string fontFamily = AppTools.GetLocalSetting(AppSettings.ReadFontFamily, "Tw Cen MT");
            string fontSize = AppTools.GetLocalSetting(AppSettings.ReadFontSize, "16");
            css = css.Replace("$FontFamily$", fontFamily).Replace("FontSize", fontSize);
            string result = html.Replace("$theme$", theme.ToLower()).Replace("$style$", css).Replace("$body$", content);
            return result;
        }
        /// <summary>
        /// 生成时间线卡片
        /// </summary>
        /// <param name="feed"></param>
        /// <returns></returns>
        private async Task GenerateActivityAsync(RssSchema feed)
        {
            try
            {
                UserActivityChannel channel = UserActivityChannel.GetDefault();
                UserActivity userActivity = await channel.GetOrCreateUserActivityAsync(feed.InternalID);
                userActivity.VisualElements.DisplayText = feed.Title;
                userActivity.VisualElements.Content = AdaptiveCardBuilder.CreateAdaptiveCardFromJson(await AppTools.CreateAdaptiveJson(feed));
                //Populate required properties
                string url = $"richasy-rss://feed?id={WebUtility.UrlEncode(feed.InternalID)}&summary={WebUtility.UrlEncode(feed.Summary)}&date={WebUtility.UrlEncode(feed.PublishDate.ToString())}&img={WebUtility.UrlEncode(feed.ImageUrl)}&url={WebUtility.UrlDecode(feed.FeedUrl)}&title={WebUtility.UrlEncode(feed.Title)}&content={WebUtility.UrlEncode(feed.Content)}";
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
        /// <summary>
        /// 检查返回频道按钮状态，正常则将当前页面的文章列表再送回去
        /// </summary>
        public void CheckBack()
        {
            if (GridViewButton.Visibility == Visibility.Visible)
            {
                MainPage.Current.MainFrame.Navigate(typeof(ChannelDetailPage), AllFeeds);
            }
        }

        private async void FeedListView_ItemClick(object sender, ItemClickEventArgs e)
        {
            var data = e.ClickedItem as RssSchema;
            
            bool isUnread = Convert.ToBoolean(AppTools.GetLocalSetting(AppSettings.IsJustUnread, "False"));
            if (!isUnread)
            {
                ShowFeeds.Insert(0, _sourceFeed);
            }
            ShowFeeds.Remove(data);
            _sourceFeed = data;
            LoadingRing.IsActive = true;
            if (MainPage.Current.ChannelListView.SelectedItem != null)
            {
                var selectChannel = MainPage.Current.ChannelListView.SelectedItem as Channel;
                if (MainPage.Current.ReadableList.Any(c => c.Id == selectChannel.Id))
                {
                    DetailWebView.NavigateToString("");
                    
                    SmartReader.Article article = await SmartReader.Reader.ParseArticleAsync(_sourceFeed.FeedUrl);
                    if (article.IsReadable || !string.IsNullOrEmpty(article.TextContent))
                    {
                        _sourceFeed.Content = article.Content;
                    }
                    else
                    {
                        new PopupToast(AppTools.GetReswLanguage("Tip_ReadError"), AppTools.GetThemeSolidColorBrush(ColorType.ErrorColor)).ShowPopup();
                    }
                    
                }
            }
            await UpdateFeed();
            LoadingRing.IsActive = false;
        }
        private async Task UpdateFeed()
        {
            //ShowFeeds.Clear();
            //bool isUnread = Convert.ToBoolean(AppTools.GetLocalSetting(AppSettings.IsJustUnread, "False"));
            //foreach (var item in AllFeeds)
            //{
            //    if (item.InternalID != _sourceFeed.InternalID)
            //    {
            //        if (isUnread && !MainPage.Current.ReadIds.Contains(item.InternalID))
            //            ShowFeeds.Add(item);
            //        else if (!isUnread)
            //            ShowFeeds.Add(item);
            //    }
            //}
            ButtonStatusCheck();
            TitleTextBlock.Text = _sourceFeed.Title;
            string html = await PackageHTML(_sourceFeed.Content);
            LoadingRing.IsActive = true;
            DetailWebView.NavigateToString(html);
            bool isSideLocked = Convert.ToBoolean(AppTools.GetLocalSetting(AppSettings.SideListLocked, "False"));
            if (MainPage.Current.MinsizeHeaderContainer.Visibility == Visibility.Visible && !isSideLocked)
            {
                DetailSplitView.IsPaneOpen = false;
            }
            MainPage.Current.AddReadId(_sourceFeed.InternalID);
            await GenerateActivityAsync(_sourceFeed);
        }
        private void GridViewButton_Click(object sender, RoutedEventArgs e)
        {
            if (MainPage.Current.MainFrame.CanGoBack)
            {
                MainPage.Current.MainFrame.GoBack();
            }
        }

        private void MoreButton_Click(object sender, RoutedEventArgs e)
        {
            FlyoutBase.ShowAttachedFlyout(sender as FrameworkElement);
        }

        private void IndexPage_DataRequested(DataTransferManager sender, DataRequestedEventArgs args)
        {
            //创建一个数据包
            DataPackage dataPackage = new DataPackage();
            //把要分享的链接放到数据包里
            dataPackage.SetHtmlFormat(HtmlFormatHelper.CreateHtmlFormat(_tempHtml));
            dataPackage.SetWebLink(new Uri(_sourceFeed.FeedUrl));
            //数据包的标题（内容和标题必须提供）
            dataPackage.Properties.Title = _sourceFeed.Title;
            //数据包的描述
            dataPackage.Properties.Description = _sourceFeed.Summary;
            //给dataRequest对象赋值
            DataRequest request = args.Request;
            request.Data = dataPackage;
        }
        private void SelectText_DataRequested(DataTransferManager sender, DataRequestedEventArgs args)
        {
            //创建一个数据包
            DataPackage dataPackage = new DataPackage();
            //把要分享的链接放到数据包里
            dataPackage.SetText(_selectText);
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
            string html = await PackageHTML(_sourceContent);
            DetailWebView.NavigateToString(html);
        }

        private void SideListButton_Click(object sender, RoutedEventArgs e)
        {
            DetailSplitView.IsPaneOpen = !DetailSplitView.IsPaneOpen;
        }

        private void Page_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            double width = e.NewSize.Width;
            if (DetailSplitView != null && (width < 1300 || ShowFeeds.Count == 0))
            {
                LockButton.Visibility = Visibility.Visible;
                bool isSideLocked = Convert.ToBoolean(AppTools.GetLocalSetting(AppSettings.SideListLocked, "False"));
                DetailSplitView.IsPaneOpen = isSideLocked;
                if (!isSideLocked || ShowFeeds.Count == 0)
                {
                    
                    FeedListView.Visibility = Visibility.Collapsed;
                    Grid.SetColumn(SideControlContainer, 1);
                    SideControlContainer.HorizontalAlignment = HorizontalAlignment.Right;
                    SideControlContainer.Margin = new Thickness(0, 0, 10, 0);
                }
            }
            else
            {
                LockButton.Visibility = Visibility.Collapsed;
                DetailSplitView.IsPaneOpen = true;
            }
        }

        private async void AddTodoButton_Click(object sender, RoutedEventArgs e)
        {
            (sender as Button).IsEnabled = false;
            try
            {
                MainPage.Current.TodoList.Add(_sourceFeed);
                AddTodoButton.Visibility = Visibility.Collapsed;
                RemoveTodoButton.Visibility = Visibility.Visible;
                new PopupToast(AppTools.GetReswLanguage("Tip_AddTodoListSuccess")).ShowPopup();
                await IOTools.AddTodoRead(_sourceFeed);
            }
            catch (Exception ex)
            {
                new PopupToast(ex.Message, AppTools.GetThemeSolidColorBrush(ColorType.ErrorColor)).ShowPopup();
            }
            (sender as Button).IsEnabled = true;
        }

        private async void AddStarButton_Click(object sender, RoutedEventArgs e)
        {
            (sender as Button).IsEnabled = false;
            try
            {
                MainPage.Current.StarList.Add(_sourceFeed);
                AddStarButton.Visibility = Visibility.Collapsed;
                RemoveStarButton.Visibility = Visibility.Visible;
                new PopupToast(AppTools.GetReswLanguage("Tip_AddStarListSuccess")).ShowPopup();
                await IOTools.AddStar(_sourceFeed);
            }
            catch (Exception ex)
            {
                new PopupToast(ex.Message, AppTools.GetThemeSolidColorBrush(ColorType.ErrorColor)).ShowPopup();
            }
            (sender as Button).IsEnabled = true;
        }

        private async void RemoveTodoButton_Click(object sender, RoutedEventArgs e)
        {
            (sender as Button).IsEnabled = false;
            try
            {
                MainPage.Current.TodoList.RemoveAll(p=>p.Equals(_sourceFeed));
                AddTodoButton.Visibility = Visibility.Visible;
                RemoveTodoButton.Visibility = Visibility.Collapsed;
                new PopupToast(AppTools.GetReswLanguage("Tip_DeleteTodoListSuccess")).ShowPopup();
                await IOTools.DeleteTodoRead(_sourceFeed);
            }
            catch (Exception ex)
            {
                new PopupToast(ex.Message, AppTools.GetThemeSolidColorBrush(ColorType.ErrorColor)).ShowPopup();
            }
            (sender as Button).IsEnabled = true;
        }

        private async void RemoveStarButton_Click(object sender, RoutedEventArgs e)
        {
            (sender as Button).IsEnabled = false;
            try
            {
                MainPage.Current.StarList.RemoveAll(p => p.Equals(_sourceFeed));
                AddStarButton.Visibility = Visibility.Visible;
                RemoveStarButton.Visibility = Visibility.Collapsed;
                new PopupToast(AppTools.GetReswLanguage("Tip_DeleteStarListSuccess")).ShowPopup();
                await IOTools.DeleteStar(_sourceFeed);
            }
            catch (Exception ex)
            {
                new PopupToast(ex.Message, AppTools.GetThemeSolidColorBrush(ColorType.ErrorColor)).ShowPopup();
            }
            (sender as Button).IsEnabled = true;
        }

        private async void Menu_Translate_Click(object sender, RoutedEventArgs e)
        {
            if (!NetworkHelper.Instance.ConnectionInformation.IsInternetAvailable)
            {
                new PopupToast(AppTools.GetReswLanguage("Tip_FailedWithoutInternet"), AppTools.GetThemeSolidColorBrush(ColorType.ErrorColor)).ShowPopup();
                return;
            }
            string language = (sender as MenuFlyoutItem).Name.Replace("Menu_Translate_", "");
            string appId = AppTools.GetRoamingSetting(AppSettings.Translate_BaiduAppId, "");
            if (string.IsNullOrEmpty(appId))
            {
                var dialog = new Dialog.BaiduTranslateDialog();
                await dialog.ShowAsync();
            }
            appId = AppTools.GetRoamingSetting(AppSettings.Translate_BaiduAppId, "");
            string appKey = AppTools.GetRoamingSetting(AppSettings.Translate_BaiduKey, "");
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
                    new PopupToast(AppTools.GetReswLanguage("Tip_TranslateFailed"), AppTools.GetThemeSolidColorBrush(ColorType.ErrorColor)).ShowPopup();
                }
                LoadingRing.IsActive = false;
            }
        }
        protected override void OnNavigatedFrom(NavigationEventArgs e)
        {
            DetailWebView.NavigateToString("");
            this.UnloadObject(this);
            base.OnNavigatedFrom(e);
        }
        private async void DetailWebView_ScriptNotify(object sender, NotifyEventArgs e)
        {
            try
            {
                var data = JsonConvert.DeserializeObject<WebNotify>(e.Value);
                // 图片点击事件，弹出图片对话框
                if (data.Key == "ImageClick" && !string.IsNullOrEmpty(data.Value))
                {
                    var imageDialog = new ImageDialog(data.Value);
                    await imageDialog.ShowAsync();
                }
                // 文本选中事件，弹出对应菜单
                else if(data.Key=="SelectText" && !string.IsNullOrEmpty(data.Value))
                {
                    var pos = Window.Current.CoreWindow.PointerPosition;
                    double x = pos.X - Window.Current.Bounds.X;
                    double y = pos.Y - Window.Current.Bounds.Y;
                    _selectText = data.Value;
                    SelectTextFlyout.ShowAt(MainPage.Current.RootGrid, new Windows.Foundation.Point(x,y));
                }
                else if(data.Key=="LinkClick" && !string.IsNullOrEmpty(data.Value))
                {
                    try
                    {
                        await Launcher.LaunchUriAsync(new Uri(data.Value));
                    }
                    catch (Exception)
                    {
                        new PopupToast(AppTools.GetReswLanguage("Tip_LaunchUriError")).ShowPopup();
                    }
                }
            }
            catch (Exception)
            {
                return;
            }
            
        }
        private void DetailSplitView_PaneClosing(SplitView sender, SplitViewPaneClosingEventArgs args)
        {
            FeedListView.Visibility = Visibility.Collapsed;
            Grid.SetColumn(SideControlContainer, 1);
            SideControlContainer.HorizontalAlignment = HorizontalAlignment.Right;
            SideControlContainer.Margin = new Thickness(0,0,10,0);
        }

        private void DetailSplitView_PaneOpening(SplitView sender, object args)
        {
            FeedListView.Visibility = Visibility.Visible;
            Grid.SetColumn(SideControlContainer, 0);
            SideControlContainer.HorizontalAlignment = HorizontalAlignment.Center;
            SideControlContainer.Margin = new Thickness(0);
        }
        /// <summary>
        /// 选中文本翻译
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void TextMenu_Translate_Click(object sender, RoutedEventArgs e)
        {
            if (!NetworkHelper.Instance.ConnectionInformation.IsInternetAvailable)
            {
                new PopupToast(AppTools.GetReswLanguage("Tip_FailedWithoutInternet"), AppTools.GetThemeSolidColorBrush(ColorType.ErrorColor)).ShowPopup();
                return;
            }
            string language = (sender as MenuFlyoutItem).Name.Replace("SelectMenu_Translate_", "");
            string appId = AppTools.GetRoamingSetting(AppSettings.Translate_BaiduAppId, "");
            string appKey = AppTools.GetRoamingSetting(AppSettings.Translate_BaiduKey, "");
            if (string.IsNullOrEmpty(appId))
            {
                new PopupToast(AppTools.GetReswLanguage("Tip_NeedLinkTranslateService"), AppTools.GetThemeSolidColorBrush(ColorType.ErrorColor)).ShowPopup();
                return;
            }
            else
            {
                LoadingRing.IsActive = true;
                string output = await TranslateTools.Translate(_selectText, appId, appKey, "auto", language.ToLower());
                if (!string.IsNullOrEmpty(output))
                {
                    new PopupToast(output,AppTools.GetThemeSolidColorBrush(ColorType.SpecialColor)).ShowPopup();
                }
                else
                {
                    new PopupToast(AppTools.GetReswLanguage("Tip_TranslateFailed"), AppTools.GetThemeSolidColorBrush(ColorType.ErrorColor)).ShowPopup();
                }
                LoadingRing.IsActive = false;
            }
        }

        private async void SelectMenu_SearchText_Click(object sender, RoutedEventArgs e)
        {
            string searchEngine = AppTools.GetRoamingSetting(AppSettings.SearchEngine, "Bing");
            string url = "";
            string content = WebUtility.UrlEncode(_selectText);
            switch (searchEngine)
            {
                case "Google":
                    url = $"https://www.google.com/search?q={content}";
                    break;
                case "Baidu":
                    url = $"https://www.baidu.com/s?wd={content}";
                    break;
                case "Bing":
                    url = $"https://cn.bing.com/search?q={content}";
                    break;
                default:
                    break;
            }
            if (!string.IsNullOrEmpty(url))
            {
                await Launcher.LaunchUriAsync(new Uri(url));
            }
        }

        private void SelectMenu_Share_Click(object sender, RoutedEventArgs e)
        {
            DataTransferManager dataTransferManager = DataTransferManager.GetForCurrentView();
            dataTransferManager.DataRequested += SelectText_DataRequested;
            DataTransferManager.ShowShareUI();
        }

        private async void WebButton_Click(object sender, RoutedEventArgs e)
        {
            //if (_sourceFeed.FeedUrl.Contains("www.ithome.com"))
            //{
            //    string link = _sourceFeed.FeedUrl.Replace("https://www.ithome.com/0/", "").Replace(".htm", "");
            //    link = link.Replace("/", "");
            //    link = $"ithome://news?id={link}";
            //    bool result = await Launcher.LaunchUriAsync(new Uri(link));
            //    if (result)
            //    {
            //        return;
            //    }
            //}
            var opt=new LauncherOptions();
            opt.IgnoreAppUriHandlers = true;
            await Launcher.LaunchUriAsync(new Uri(_sourceFeed.FeedUrl),opt);
        }

        private async void SelectMenu_Speech_Click(object sender, RoutedEventArgs e)
        {
            var regex1 = new Regex(@"<\s*br[^>]?>");
            var regex2 = new Regex(@"(<([^>]+)>)");
            string text = regex1.Replace(_selectText, "\n");
            text = regex2.Replace(text, "");
            if (!string.IsNullOrEmpty(text))
            {
                MediaControlButton.Visibility = Visibility.Visible;
                await SpeakTextAsync(text);
            }
        }

        private async void Menu_Speech_Click(object sender, RoutedEventArgs e)
        {
            var regex1 = new Regex(@"<\s*br[^>]?>");
            var regex2 = new Regex(@"(<([^>]+)>)");
            string text = regex1.Replace(_sourceFeed.Content??_sourceFeed.Summary, "\n");
            text = regex2.Replace(text, "");
            if (!string.IsNullOrEmpty(text))
            {
                MediaControlButton.Visibility = Visibility.Visible;
                await SpeakTextAsync(text);
            }
        }

        private void MediaControlButton_Click(object sender, RoutedEventArgs e)
        {
            VoiceMediaElement.Stop();
            MediaControlButton.Visibility = Visibility.Collapsed;
        }
        /// <summary>
        /// 获取原网页并解析为可读文本
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void ReadabilityButton_Click(object sender, RoutedEventArgs e)
        {
            if (!NetworkHelper.Instance.ConnectionInformation.IsInternetAvailable)
            {
                new PopupToast(AppTools.GetReswLanguage("Tip_FailedWithoutInternet"), AppTools.GetThemeSolidColorBrush(ColorType.ErrorColor)).ShowPopup();
                return;
            }
            LoadingRing.IsActive = true;
            ReadabilityButton.IsEnabled = false;
            SmartReader.Article article = await SmartReader.Reader.ParseArticleAsync(_sourceFeed.FeedUrl);
            if (article.IsReadable || !string.IsNullOrEmpty(article.TextContent))
            {
                string content=await PackageHTML(article.Content??article.TextContent);
                _sourceFeed.Content = article.Content;
                DetailWebView.NavigateToString(content);
            }
            else
            {
                new PopupToast(AppTools.GetReswLanguage("Tip_ReadError"),AppTools.GetThemeSolidColorBrush(ColorType.ErrorColor)).ShowPopup();
            }
            ReadabilityButton.IsEnabled = true;
            LoadingRing.IsActive = false;
        }

        private async void SelectMenu_Mark_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                await DetailWebView.InvokeScriptAsync("setMark", new string[] { });
            }
            catch (Exception)
            {
                new PopupToast(AppTools.GetReswLanguage("Tip_DoNotMarkAgain"), AppTools.GetThemeSolidColorBrush(ColorType.ErrorColor)).ShowPopup();
            }
            
        }

        private async void ShareButton_Click(object sender, RoutedEventArgs e)
        {
            _tempHtml = await DetailWebView.InvokeScriptAsync("getHtml", new string[] { });
            DataTransferManager dataTransferManager = DataTransferManager.GetForCurrentView();
            dataTransferManager.DataRequested += IndexPage_DataRequested;
            DataTransferManager.ShowShareUI();
        }

        private void DetailWebView_DOMContentLoaded(WebView sender, WebViewDOMContentLoadedEventArgs args)
        {
            LoadingRing.IsActive = false;
        }

        private void SettingButton_Click(object sender, RoutedEventArgs e)
        {
            FlyoutBase.ShowAttachedFlyout(sender as FrameworkElement);
        }

        private async void FontFamilyComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!_isInit)
                return;
            string name = (FontFamilyComboBox.SelectedItem as SystemFont).Name;
            AppTools.WriteLocalSetting(AppSettings.ReadFontFamily, name);
            await DetailWebView.InvokeScriptAsync("setFontFamily", new string[] { name });
        }

        private async void FontSizeTextBox_LosingFocus(UIElement sender, Windows.UI.Xaml.Input.LosingFocusEventArgs args)
        {
            await SetFontSize();
        }
        private async Task SetFontSize()
        {
            if (!_isInit)
                return;
            string oldSize = AppTools.GetLocalSetting(AppSettings.ReadFontSize, "16");
            string size = FontSizeTextBox.Text?.Trim();
            if (oldSize == size)
            {
                return;
            }
            if (string.IsNullOrEmpty(size))
            {
                FontSizeTextBox.Text = oldSize;
                return;
            }
            try
            {
                int s = Convert.ToInt32(size);
                if (s > 0)
                {
                    await DetailWebView.InvokeScriptAsync("setFontSize", new string[] { s.ToString() });
                    AppTools.WriteLocalSetting(AppSettings.ReadFontSize, s.ToString());
                    FontSizeTextBox.Text = s.ToString();
                    return;
                }
            }
            catch (Exception)
            {
            }
            FontSizeTextBox.Text = oldSize;
            return;
        }

        private async void FontSizeTextBox_KeyDown(object sender, Windows.UI.Xaml.Input.KeyRoutedEventArgs e)
        {
            if (e.Key == VirtualKey.Enter)
            {
                await SetFontSize();
            }
        }

        private void LockButton_Click(object sender, RoutedEventArgs e)
        {
            bool isSideLocked = Convert.ToBoolean(AppTools.GetLocalSetting(AppSettings.SideListLocked, "False"));
            if (isSideLocked)
            {
                LockButton.Foreground = AppTools.GetThemeSolidColorBrush(ColorType.ImportantTextColor);
            }
            else
            {
                LockButton.Foreground = AppTools.GetThemeSolidColorBrush(ColorType.PrimaryColor);
            }
            AppTools.WriteLocalSetting(AppSettings.SideListLocked, (!isSideLocked).ToString());
        }
    }
}
