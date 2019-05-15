using RSS_Stalker.Controls;
using CoreLib.Enums;
using CoreLib.Tools;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.ApplicationModel;
using Windows.ApplicationModel.Activation;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Globalization;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using RSS_Stalker.Pages;

namespace RSS_Stalker
{
    /// <summary>
    /// 提供特定于应用程序的行为，以补充默认的应用程序类。
    /// </summary>
    sealed partial class App : Application
    {
        /// <summary>
        /// 初始化单一实例应用程序对象。这是执行的创作代码的第一行，
        /// 已执行，逻辑上等同于 main() 或 WinMain()。
        /// </summary>
        /// 
        public static OneDriveTools OneDrive=new OneDriveTools();
        public App()
        {
            ChangeLanguage();
            this.InitializeComponent();
            this.Suspending += OnSuspending;
            RequestedTheme = AppTools.GetRoamingSetting(AppSettings.Theme,"Light") == "Light" ? ApplicationTheme.Light : ApplicationTheme.Dark;
            UnhandledException += UnhandleExceptionHandle;
        }

        private void UnhandleExceptionHandle(object sender, Windows.UI.Xaml.UnhandledExceptionEventArgs e)
        {
            e.Handled = true;
            string msg = e.Exception.Message;
            new PopupToast(msg, AppTools.GetThemeSolidColorBrush(ColorType.ErrorColor)).ShowPopup();
        }
        /// <summary>
        /// 更改语言首选项
        /// </summary>
        private void ChangeLanguage()
        {
            string lan = AppTools.GetRoamingSetting(AppSettings.Language,"en_US");

            if (lan == "")
            {
                var Languages = Windows.System.UserProfile.GlobalizationPreferences.Languages;
                if (Languages.Count > 0)
                {
                    var language = Languages[0];
                    if (language.ToLower().IndexOf("zh") != -1)
                    {
                        AppTools.WriteRoamingSetting(AppSettings.Language, "zh_CN");
                        //if (language.ToLower() == "zh-hans-cn")
                        //{

                        //}
                        //else
                        //{
                        //    AppTools.WriteLocalSetting(AppSettings.Language, "Tw");
                        //}
                    }
                    else
                    {
                        AppTools.WriteRoamingSetting(AppSettings.Language, "en_US");
                    }
                }
                else
                {
                    AppTools.WriteRoamingSetting(AppSettings.Language, "en_US");
                }
            }
            lan = AppTools.GetRoamingSetting(AppSettings.Language,"en_US");
            string code = "";
            switch (lan)
            {
                case "zh_CN":
                    code = "zh-CN";
                    break;
                case "en_US":
                    code = "en-US";
                    break;
                default:
                    code = "en-US";
                    break;
            }
            ApplicationLanguages.PrimaryLanguageOverride = code;
        }
        protected override void OnActivated(IActivatedEventArgs args)
        {
            Frame rootFrame = Window.Current.Content as Frame;

            // 不要在窗口已包含内容时重复应用程序初始化，
            // 只需确保窗口处于活动状态
            if (rootFrame == null)
            {
                // 创建要充当导航上下文的框架，并导航到第一页
                rootFrame = new Frame();
                bool isBinding = Convert.ToBoolean(AppTools.GetLocalSetting(AppSettings.IsBindingOneDrive, "False"));
                bool isLocal = Convert.ToBoolean(AppTools.GetLocalSetting(AppSettings.IsLocalAccount, "False"));
                if (isBinding || isLocal)
                    rootFrame.Navigate(typeof(MainPage),"Timeline");
                else
                {
                    rootFrame.Navigate(typeof(Pages.OneDrivePage));
                    return;
                }
                rootFrame.NavigationFailed += OnNavigationFailed;
                rootFrame.Loaded += (_s, _e) =>
                {
                    if (args.Kind == ActivationKind.Protocol)
                    {
                        var uriArgs = args as ProtocolActivatedEventArgs;
                        if (uriArgs != null)
                        {
                            string[] query = uriArgs.Uri.Query.Split('&');
                            OpenContentFromTimeline(query);
                        }
                    }
                };
                // 将框架放在当前窗口中
                Window.Current.Content = rootFrame;
                Window.Current.Activate();
            }
            else
            {
                bool isBinding = Convert.ToBoolean(AppTools.GetLocalSetting(AppSettings.IsBindingOneDrive, "False"));
                bool isLocal = Convert.ToBoolean(AppTools.GetLocalSetting(AppSettings.IsLocalAccount, "False"));
                if (!isBinding && !isLocal)
                    return;
                if (args.Kind == ActivationKind.Protocol)
                {
                    var uriArgs = args as ProtocolActivatedEventArgs;
                    if (uriArgs != null)
                    {
                        string[] query = uriArgs.Uri.Query.Split('&');
                        OpenContentFromTimeline(query);
                    }
                }
            }

        }
        /// <summary>
        /// 在应用程序由最终用户正常启动时进行调用。
        /// 将在启动应用程序以打开特定文件等情况下使用。
        /// </summary>
        /// <param name="e">有关启动请求和过程的详细信息。</param>
        protected override void OnLaunched(LaunchActivatedEventArgs e)
        {
            
            Frame rootFrame = Window.Current.Content as Frame;
            // 不要在窗口已包含内容时重复应用程序初始化，
            // 只需确保窗口处于活动状态
            if (rootFrame == null)
            {
                // 创建要充当导航上下文的框架，并导航到第一页
                rootFrame = new Frame();

                rootFrame.NavigationFailed += OnNavigationFailed;

                if (e.PreviousExecutionState == ApplicationExecutionState.Terminated)
                {
                    //TODO: 从之前挂起的应用程序加载状态
                }

                // 将框架放在当前窗口中
                Window.Current.Content = rootFrame;
            }
            
            if (e.PrelaunchActivated == false)
            {
                if (rootFrame.Content == null)
                {
                    // 当导航堆栈尚未还原时，导航到第一页，
                    // 并通过将所需信息作为导航参数传入来配置
                    // 参数
                    bool isBinding = Convert.ToBoolean(AppTools.GetLocalSetting(AppSettings.IsBindingOneDrive, "False"));
                    bool isLocal = Convert.ToBoolean(AppTools.GetLocalSetting(AppSettings.IsLocalAccount, "False"));
                    bool isSyncOneDrive = Convert.ToBoolean(AppTools.GetLocalSetting(AppSettings.SyncWithStart, "False"));
                    if (isBinding)
                    {
                        if (isSyncOneDrive)
                        {
                            double lastTime = Convert.ToDouble(AppTools.GetLocalSetting(AppSettings.LastSyncTime, "0"));
                            double now = AppTools.DateToTimeStamp(DateTime.Now.ToLocalTime());
                            if (now - 10800 > lastTime)
                                rootFrame.Navigate(typeof(SplashPage));
                            else
                                rootFrame.Navigate(typeof(MainPage), e.Arguments);
                        }
                        else
                            rootFrame.Navigate(typeof(MainPage), e.Arguments);
                    }
                    else if (isLocal)
                        rootFrame.Navigate(typeof(MainPage), e.Arguments);
                    else
                        rootFrame.Navigate(typeof(OneDrivePage));
                }
                // 确保当前窗口处于活动状态
                Window.Current.Activate();
            }
        }
        /// <summary>
        /// 解析Timeline卡片的链接
        /// </summary>
        /// <param name="query"></param>
        private void OpenContentFromTimeline(string[] query)
        {
            string id = query.Where(p => p.StartsWith("?id")).FirstOrDefault();
            string title = query.Where(p => p.StartsWith("title")).FirstOrDefault();
            string content=query.Where(p=>p.StartsWith("content")).FirstOrDefault();
            string url = query.Where(p => p.StartsWith("url")).FirstOrDefault();
            string img = query.Where(p => p.StartsWith("img")).FirstOrDefault();
            string date = query.Where(p => p.StartsWith("date")).FirstOrDefault();
            string summary = query.Where(p => p.StartsWith("summary")).FirstOrDefault();
            if (string.IsNullOrEmpty(id) || string.IsNullOrEmpty(title) || string.IsNullOrEmpty(content) || string.IsNullOrEmpty(url))
            {
                return;
            }
            id = id.Substring(4);
            title = WebUtility.UrlDecode(title.Substring(6));
            content = WebUtility.UrlDecode(content.Substring(8));
            url = WebUtility.UrlDecode(url.Substring(4));
            img = WebUtility.UrlDecode(img.Substring(4));
            date = WebUtility.UrlDecode(date.Substring(5));
            summary = WebUtility.UrlDecode(summary.Substring(8));
            if (MainPage.Current != null)
            {
                MainPage.Current.MainFrame.Navigate(typeof(FeedDetailPage), new string[] { id, title, content,url,img,date,summary });
                MainPage.Current.ChannelListView.SelectedIndex = -1;
            }
        }
        /// <summary>
        /// 导航到特定页失败时调用
        /// </summary>
        ///<param name="sender">导航失败的框架</param>
        ///<param name="e">有关导航失败的详细信息</param>
        void OnNavigationFailed(object sender, NavigationFailedEventArgs e)
        {
            throw new Exception("Failed to load Page " + e.SourcePageType.FullName);
        }

        /// <summary>
        /// 在将要挂起应用程序执行时调用。  在不知道应用程序
        /// 无需知道应用程序会被终止还是会恢复，
        /// 并让内存内容保持不变。
        /// </summary>
        /// <param name="sender">挂起的请求的源。</param>
        /// <param name="e">有关挂起请求的详细信息。</param>
        private void OnSuspending(object sender, SuspendingEventArgs e)
        {
            var deferral = e.SuspendingOperation.GetDeferral();
            //TODO: 保存应用程序状态并停止任何后台活动
            deferral.Complete();
        }
    }
}
