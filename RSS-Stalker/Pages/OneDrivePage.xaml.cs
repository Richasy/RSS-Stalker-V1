using RSS_Stalker.Controls;
using CoreLib.Tools;
using System;
using System.Collections.Generic;
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
using CoreLib.Enums;
using RSS_Stalker.Tools;
using System.Threading.Tasks;
using Windows.Storage;
using RSS_Stalker.Dialog;
using Windows.ApplicationModel;

// https://go.microsoft.com/fwlink/?LinkId=234238 上介绍了“空白页”项模板

namespace RSS_Stalker.Pages
{
    /// <summary>
    /// 可用于自身或导航至 Frame 内部的空白页。
    /// </summary>
    public sealed partial class OneDrivePage : Page
    {
        public OneDrivePage()
        {
            this.InitializeComponent();
            AppTools.SetTitleBarColor();
        }
        protected override async void OnNavigatedTo(NavigationEventArgs e)
        {
            await CheckVersion();
        }
        private async void OneDirveButton_Click(object sender, RoutedEventArgs e)
        {
            ControlContainer.Visibility = Visibility.Collapsed;
            WaitingContainer.Visibility = Visibility.Visible;
            TitleIcon.Text = "";
            bool result=await App.OneDrive.OneDriveAuthorize();
            try
            {
                if (result)
                {
                    LinkLoadingRing.IsActive = false;
                    SyncLoadingContainer.Background = AppTools.GetThemeSolidColorBrush(ColorType.PrimaryColor);
                    SyncLoadingText.Foreground = AppTools.GetThemeSolidColorBrush(ColorType.PrimaryInsideColor);
                    SyncLoadingRing.IsActive = true;
                    new PopupToast(AppTools.GetReswLanguage("Tip_BindingOneDriveSuccess")).ShowPopup();
                    var tasks = new List<Task>();
                    var cate = Task.Run(async () =>
                    {
                        var categoryList = await App.OneDrive.GetCategoryList();
                        await IOTools.ReplaceCategory(categoryList);
                    });
                    var todo = Task.Run(async () =>
                    {
                        var TodoList = await App.OneDrive.GetTodoList();
                        await IOTools.ReplaceTodo(TodoList);
                    });
                    var star = Task.Run(async () =>
                    {
                        var StarList = await App.OneDrive.GetStarList();
                        await IOTools.ReplaceStar(StarList);
                    });
                    var toast = Task.Run(async () =>
                    {
                        var ToastList = await App.OneDrive.GetToastList();
                        await IOTools.ReplaceToast(ToastList);
                    });
                    var page = Task.Run(async () =>
                    {
                        var PageList = await App.OneDrive.GetPageList();
                        await IOTools.ReplacePage(PageList);
                    });
                    var read = Task.Run(async () =>
                    {
                        var ReadList = await App.OneDrive.GetReadList();
                        await IOTools.ReplaceReadIds(ReadList);
                    });
                    tasks.Add(cate);
                    tasks.Add(todo);
                    tasks.Add(star);
                    tasks.Add(toast);
                    tasks.Add(page);
                    tasks.Add(read);
                    await Task.WhenAll(tasks.ToArray());
                    string basicUpdateTime = AppTools.GetRoamingSetting(AppSettings.BasicUpdateTime, "1");
                    string todoUpdateTime = AppTools.GetRoamingSetting(AppSettings.TodoUpdateTime, "1");
                    string starUpdateTime = AppTools.GetRoamingSetting(AppSettings.StarUpdateTime, "1");
                    string toastUpdateTime = AppTools.GetRoamingSetting(AppSettings.ToastUpdateTime, "1");
                    string pageUpdateTime = AppTools.GetRoamingSetting(AppSettings.PageUpdateTime, "1");
                    string readUpdateTime = AppTools.GetRoamingSetting(AppSettings.ReadUpdateTime, "1");
                    AppTools.WriteLocalSetting(AppSettings.ToastUpdateTime, toastUpdateTime);
                    AppTools.WriteLocalSetting(AppSettings.StarUpdateTime, starUpdateTime);
                    AppTools.WriteLocalSetting(AppSettings.TodoUpdateTime, todoUpdateTime);
                    AppTools.WriteLocalSetting(AppSettings.BasicUpdateTime, basicUpdateTime);
                    AppTools.WriteLocalSetting(AppSettings.PageUpdateTime, pageUpdateTime);
                    AppTools.WriteLocalSetting(AppSettings.PageUpdateTime, readUpdateTime);
                    AppTools.WriteLocalSetting(AppSettings.IsBindingOneDrive, "True");
                    var frame = Window.Current.Content as Frame;
                    frame.Navigate(typeof(MainPage));
                }
                else
                {
                    TitleIcon.Text = "";
                    ControlContainer.Visibility = Visibility.Visible;
                    WaitingContainer.Visibility = Visibility.Collapsed;
                    new PopupToast(AppTools.GetReswLanguage("Tip_BindingOneDriveFailed"), AppTools.GetThemeSolidColorBrush(ColorType.ErrorColor)).ShowPopup();
                }
            }
            catch (Exception ex)
            {
                TitleIcon.Text = "";
                ControlContainer.Visibility = Visibility.Visible;
                WaitingContainer.Visibility = Visibility.Collapsed;
                LinkLoadingRing.IsActive = true;
                SyncLoadingRing.IsActive = false;
                SyncLoadingContainer.Background = AppTools.GetThemeSolidColorBrush(ColorType.LineColor);
                SyncLoadingText.Foreground = AppTools.GetThemeSolidColorBrush(ColorType.NormalTextColor);
                new PopupToast(ex.Message,AppTools.GetThemeSolidColorBrush(ColorType.ErrorColor)).ShowPopup();
            }
        }

        private async void LocalButton_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new Dialog.ConfirmDialog(AppTools.GetReswLanguage("Tip_LocalLoginTitle"), AppTools.GetReswLanguage("Tip_LocalLoginTip"));
            var result = await dialog.ShowAsync();
            if (result == ContentDialogResult.Primary)
            {
                AppTools.WriteLocalSetting(AppSettings.IsLocalAccount, "True");
                var frame = Window.Current.Content as Frame;
                frame.Navigate(typeof(MainPage));
            }
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
                string lan = AppTools.GetRoamingSetting(AppSettings.Language, "en_US");
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
    }
}
