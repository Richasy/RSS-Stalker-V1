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
using CoreLib.Enums;

// https://go.microsoft.com/fwlink/?LinkId=234238 上介绍了“内容对话框”项模板

namespace RSS_Stalker.Dialog
{
    /// <summary>
    /// 修改分类对话框
    /// </summary>
    public sealed partial class ModifyCategoryDialog : ContentDialog
    {
        private Category _sourceCategory;
        public ObservableCollection<string> IconCollection = new ObservableCollection<string>();
        public static ModifyCategoryDialog Current;
        public ModifyCategoryDialog(Category data)
        {
            this.InitializeComponent();
            _sourceCategory = data;
            Current = this;
            var list = AppTools.GetIcons();
            foreach (var item in list)
            {
                IconCollection.Add(item);
            }
            IconTextBlock.Text = data.Icon;
            CategoryNameTextBox.Text = data.Name;
            Title = AppTools.GetReswLanguage("Tip_UpdateCategory");
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
                _sourceCategory.Name=name;
                _sourceCategory.Icon = icon;
                await IOTools.UpdateCategory(_sourceCategory);
                new PopupToast(AppTools.GetReswLanguage("Tip_UpdateCategorySuccess")).ShowPopup();
                foreach (var item in MainPage.Current.Categories)
                {
                    if (item.Id == _sourceCategory.Id)
                    {
                        item.Name = _sourceCategory.Name;
                        item.Icon = _sourceCategory.Icon;
                    }
                }
                Hide();
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
            IconFlyout.Hide();
            IconTextBlock.Text = str;
        }
    }
}
