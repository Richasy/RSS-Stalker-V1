using RSS_Stalker.Controls;
using RSS_Stalker.Tools;
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
                var categoryList = await App.OneDrive.GetCategoryList();
                await IOTools.ReplaceCategory(categoryList);
                var TodoList = await App.OneDrive.GetTodoList();
                await IOTools.ReplaceTodo(TodoList);
                var StarList = await App.OneDrive.GetStarList();
                await IOTools.ReplaceStar(StarList);
                string basicUpdateTime = AppTools.GetRoamingSetting(Enums.AppSettings.BasicUpdateTime, "1");
                string todoUpdateTime = AppTools.GetRoamingSetting(Enums.AppSettings.TodoUpdateTime, "1");
                string starUpdateTime = AppTools.GetRoamingSetting(Enums.AppSettings.StarUpdateTime, "1");
                AppTools.WriteLocalSetting(Enums.AppSettings.BasicUpdateTime, basicUpdateTime);
                AppTools.WriteLocalSetting(Enums.AppSettings.TodoUpdateTime, todoUpdateTime);
                AppTools.WriteLocalSetting(Enums.AppSettings.StarUpdateTime, starUpdateTime);
                AppTools.WriteLocalSetting(Enums.AppSettings.IsBindingOneDrive, "True");
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
