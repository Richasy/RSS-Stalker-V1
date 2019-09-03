using CoreLib.Enums;
using CoreLib.Tools;
using RSS_Stalker.Tools;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.ApplicationModel.Activation;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Imaging;
using Windows.UI.Xaml.Navigation;

// https://go.microsoft.com/fwlink/?LinkId=234238 上介绍了“空白页”项模板

namespace RSS_Stalker.Pages
{
    /// <summary>
    /// 可用于自身或导航至 Frame 内部的空白页。
    /// </summary>
    public sealed partial class SplashPage : Page
    {
        /// <summary>
        /// 伪装的启动页，用以进行OneDrive数据同步
        /// </summary>
        public SplashPage()
        {
            this.InitializeComponent();
            string theme = AppTools.GetRoamingSetting(AppSettings.Theme, "Light");
            var image = new BitmapImage(new Uri($"ms-appx:///Assets/{theme}.png"));
            AppIcon.Source = image;
            Sync();
        }
        private async void Sync()
        {
            bool result = await App.OneDrive.OneDriveAuthorize();
            if (result)
            {
                int now = AppTools.DateToTimeStamp(DateTime.Now.ToLocalTime());
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
                var readable = Task.Run(async () =>
                {
                    var ReadList = await App.OneDrive.GetReadableList();
                    await IOTools.ReplaceReadable(ReadList);
                });
                tasks.Add(cate);
                tasks.Add(todo);
                tasks.Add(star);
                tasks.Add(toast);
                tasks.Add(page);
                tasks.Add(read);
                tasks.Add(readable);
                try
                {
                    await Task.WhenAll(tasks.ToArray());
                    string basicUpdateTime = AppTools.GetRoamingSetting(AppSettings.BasicUpdateTime, "1");
                    string todoUpdateTime = AppTools.GetRoamingSetting(AppSettings.TodoUpdateTime, "1");
                    string starUpdateTime = AppTools.GetRoamingSetting(AppSettings.StarUpdateTime, "1");
                    string toastUpdateTime = AppTools.GetRoamingSetting(AppSettings.ToastUpdateTime, "1");
                    string pageUpdateTime = AppTools.GetRoamingSetting(AppSettings.PageUpdateTime, "1");
                    string readUpdateTime = AppTools.GetRoamingSetting(AppSettings.ReadUpdateTime, "1");
                    string readableUpdateTime = AppTools.GetRoamingSetting(AppSettings.ReadableUpdateTime, "1");
                    AppTools.WriteLocalSetting(AppSettings.ToastUpdateTime, toastUpdateTime);
                    AppTools.WriteLocalSetting(AppSettings.StarUpdateTime, starUpdateTime);
                    AppTools.WriteLocalSetting(AppSettings.TodoUpdateTime, todoUpdateTime);
                    AppTools.WriteLocalSetting(AppSettings.BasicUpdateTime, basicUpdateTime);
                    AppTools.WriteLocalSetting(AppSettings.PageUpdateTime, pageUpdateTime);
                    AppTools.WriteLocalSetting(AppSettings.ReadUpdateTime, readUpdateTime);
                    AppTools.WriteLocalSetting(AppSettings.ReadableUpdateTime, readableUpdateTime);
                    AppTools.WriteLocalSetting(AppSettings.LastSyncTime, now.ToString());
                }
                catch (Exception)
                {

                }
                finally
                {
                    var frame = Window.Current.Content as Frame;
                    frame.Navigate(typeof(MainPage));
                }
            }
            else
            {
                var frame = Window.Current.Content as Frame;
                frame.Navigate(typeof(MainPage));
            }
        }
    }
}
