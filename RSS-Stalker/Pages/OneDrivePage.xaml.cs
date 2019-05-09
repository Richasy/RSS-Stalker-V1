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

        private async void OneDirveButton_Click(object sender, RoutedEventArgs e)
        {
            OneDirveButton.IsEnabled = false;
            OneDirveButton.Content = AppTools.GetReswLanguage("Tip_Waiting");
            bool result=await App.OneDrive.OneDriveAuthorize();
            if (result)
            {
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
                tasks.Add(cate);
                tasks.Add(todo);
                tasks.Add(star);
                tasks.Add(toast);
                Task.WaitAll(tasks.ToArray());
                string basicUpdateTime = AppTools.GetRoamingSetting(AppSettings.BasicUpdateTime, "1");
                string todoUpdateTime = AppTools.GetRoamingSetting(AppSettings.TodoUpdateTime, "1");
                string starUpdateTime = AppTools.GetRoamingSetting(AppSettings.StarUpdateTime, "1");
                string toastUpdateTime = AppTools.GetRoamingSetting(AppSettings.ToastUpdateTime, "1");
                AppTools.WriteLocalSetting(AppSettings.BasicUpdateTime, basicUpdateTime);
                AppTools.WriteLocalSetting(AppSettings.TodoUpdateTime, todoUpdateTime);
                AppTools.WriteLocalSetting(AppSettings.StarUpdateTime, starUpdateTime);
                AppTools.WriteLocalSetting(AppSettings.ToastUpdateTime, toastUpdateTime);
                AppTools.WriteLocalSetting(AppSettings.IsBindingOneDrive, "True");
                var frame = Window.Current.Content as Frame;
                frame.Navigate(typeof(MainPage));
            }
            else
            {
                OneDirveButton.IsEnabled = true;
                OneDirveButton.Content = AppTools.GetReswLanguage("Tip_LinkToOneDrive");
                new PopupToast(AppTools.GetReswLanguage("Tip_BindingOneDriveFailed")).ShowPopup();
            }
        }
    }
}
