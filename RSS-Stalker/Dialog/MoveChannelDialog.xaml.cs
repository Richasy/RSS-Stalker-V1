using RSS_Stalker.Controls;
using CoreLib.Models;
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
using RSS_Stalker.Tools;
using CoreLib.Enums;

// https://go.microsoft.com/fwlink/?LinkId=234238 上介绍了“内容对话框”项模板

namespace RSS_Stalker.Dialog
{
    public sealed partial class MoveChannelDialog : ContentDialog
    {
        private Channel _sourceChannel;
        /// <summary>
        /// 移动频道对话框
        /// </summary>
        /// <param name="data">数据</param>
        public MoveChannelDialog(Channel data)
        {
            this.InitializeComponent();
            _sourceChannel = data;
            Title = AppTools.GetReswLanguage("Tip_MoveChannel");
            PrimaryButtonText = AppTools.GetReswLanguage("Tip_Confirm");
            SecondaryButtonText = AppTools.GetReswLanguage("Tip_Cancel");
        }

        private async void ContentDialog_PrimaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
        {
            args.Cancel = true;
            var selectCategory = CategoryListView.SelectedItem as Category;
            if (selectCategory == null)
            {
                new PopupToast(AppTools.GetReswLanguage("Tip_NoCategorySelected"), AppTools.GetThemeSolidColorBrush(ColorType.ErrorColor)).ShowPopup();
                return;
            }
            else
            {
                var sourceCategory = MainPage.Current.CategoryListView.SelectedItem as Category;
                
                if (sourceCategory == null)
                {
                    new PopupToast(AppTools.GetReswLanguage("Tip_NoCategorySelected"), AppTools.GetThemeSolidColorBrush(ColorType.ErrorColor)).ShowPopup();
                    return;
                }
                else
                {
                    IsPrimaryButtonEnabled = false;
                    PrimaryButtonText = AppTools.GetReswLanguage("Tip_Waiting");
                    if (selectCategory.Id != sourceCategory.Id)
                    {
                        sourceCategory.Channels.RemoveAll(p => p.Id == _sourceChannel.Id);
                        selectCategory.Channels.Add(_sourceChannel);
                        await IOTools.UpdateCategory(sourceCategory);
                        await IOTools.UpdateCategory(selectCategory);
                        
                        MainPage.Current.Channels.Remove(MainPage.Current.Channels.Where(p => p.Id == _sourceChannel.Id).FirstOrDefault());
                        MainPage.Current._channelListCount -= 1;
                    }
                    new PopupToast(AppTools.GetReswLanguage("Tip_MoveChannelSuccess")).ShowPopup();
                    Hide();
                }
            }
        }

        private void ContentDialog_SecondaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
        {
        }
    }
}
