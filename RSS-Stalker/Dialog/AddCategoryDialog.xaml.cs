using RSS_Stalker.Controls;
using CoreLib.Models;
using CoreLib.Tools;
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
using RSS_Stalker.Tools;
using System.Threading.Tasks;
using CoreLib.Enums;

// https://go.microsoft.com/fwlink/?LinkId=234238 上介绍了“内容对话框”项模板

namespace RSS_Stalker.Dialog
{
    /// <summary>
    /// 添加分类对话框
    /// </summary>
    public sealed partial class AddCategoryDialog : ContentDialog
    {
        public ObservableCollection<string> IconCollection = new ObservableCollection<string>();
        public static AddCategoryDialog Current;
        public AddCategoryDialog()
        {
            this.InitializeComponent();
            Current = this;
            var list = AppTools.GetIcons();
            foreach (var item in list)
            {
                IconCollection.Add(item);
            }
            IconTextBlock.Text = IconCollection.First();
            Title = AppTools.GetReswLanguage("Tip_AddCategory");
            PrimaryButtonText = AppTools.GetReswLanguage("Tip_Confirm");
            SecondaryButtonText = AppTools.GetReswLanguage("Tip_Cancel");
        }

        private async void ContentDialog_PrimaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
        {
            args.Cancel = true;
            string icon = IconTextBlock.Text;
            string name = CategoryNameTextBox.Text;
            if (!string.IsNullOrEmpty(icon) && !string.IsNullOrEmpty(name))
            {
                IsPrimaryButtonEnabled = false;
                PrimaryButtonText = AppTools.GetReswLanguage("Tip_Waiting");
                var cate = new Category(name, icon);
                try
                {
                    await IOTools.AddCategory(cate);
                }
                catch (Exception)
                {
                    await Task.Delay(1000);
                    await IOTools.AddCategory(cate);
                }
                finally
                {
                    new PopupToast(AppTools.GetReswLanguage("Tip_AddCategorySuccess")).ShowPopup();
                    MainPage.Current.Categories.Add(cate);
                    MainPage.Current._categoryListCount += 1;
                    Hide();
                }
            }
            else
            {
                new PopupToast(AppTools.GetReswLanguage("Tip_FieldEmpty"), AppTools.GetThemeSolidColorBrush(ColorType.ErrorColor)).ShowPopup();
            }
        }

        private void ContentDialog_SecondaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
        {
        }

        private void IconContainer_Tapped(object sender, TappedRoutedEventArgs e)
        {
            FlyoutBase.ShowAttachedFlyout((FrameworkElement)sender);
        }

        private void IconGridView_ItemClick(object sender, ItemClickEventArgs e)
        {
            var str = e.ClickedItem as string;
            IconTextBlock.Text = str;
            IconFlyout.Hide();
        }
    }
}
