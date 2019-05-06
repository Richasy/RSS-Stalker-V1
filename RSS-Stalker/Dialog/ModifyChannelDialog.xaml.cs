using RSS_Stalker.Controls;
using RSS_Stalker.Models;
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

// https://go.microsoft.com/fwlink/?LinkId=234238 上介绍了“内容对话框”项模板

namespace RSS_Stalker.Dialog
{
    public sealed partial class ModifyChannelDialog : ContentDialog
    {
        private Channel _sourceChannel;
        public ModifyChannelDialog(Channel data)
        {
            this.InitializeComponent();
            _sourceChannel = data;
            Title = AppTools.GetReswLanguage("Tip_UpdateChannel");
            PrimaryButtonText = AppTools.GetReswLanguage("Tip_Confirm");
            SecondaryButtonText = AppTools.GetReswLanguage("Tip_Cancel");
            ChannelLinkTextBox.Text = data.Link;
            ChannelNameTextBox.Text = data.Name;
            ChannelDescriptionTextBox.Text = data.Description;
        }

        private async void ContentDialog_PrimaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
        {
            args.Cancel = true;
            string name = ChannelNameTextBox.Text?.Trim();
            string des = ChannelDescriptionTextBox.Text?.Trim();
            if(string.IsNullOrEmpty(name) || string.IsNullOrEmpty(des))
            {
                new PopupToast(AppTools.GetReswLanguage("Tip_FieldEmpty")).ShowPopup();
                return;
            }
            else
            {
                _sourceChannel.Name = name;
                _sourceChannel.Description = des;
                var sourceCategory = MainPage.Current.CategoryListView.SelectedItem as Category;
                if (sourceCategory != null)
                {
                    IsPrimaryButtonEnabled = false;
                    PrimaryButtonText = AppTools.GetReswLanguage("Tip_Waiting");
                    foreach (var item in sourceCategory.Channels)
                    {
                        if (item.Link == _sourceChannel.Link)
                        {
                            item.Name = name;
                            item.Description = des;
                        }
                    }
                    await IOTools.UpdateCategory(sourceCategory);
                    if(MainPage.Current.MainFrame.Content is Pages.ChannelDetailPage)
                    {
                        if (Pages.ChannelDetailPage.Current._sourceData.Link == _sourceChannel.Link)
                        {
                            Pages.ChannelDetailPage.Current.ChannelNameTextBlock.Text = name;
                            Pages.ChannelDetailPage.Current.ChannelDescriptionTextBlock.Text = des;
                        }
                    }
                    new PopupToast(AppTools.GetReswLanguage("UpdateChannelSuccess")).ShowPopup();
                    Hide();
                }
                else
                {
                    new PopupToast(AppTools.GetReswLanguage("Tip_NoCategorySelected")).ShowPopup();
                    return;
                }
            }
        }

        private void ContentDialog_SecondaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
        {
        }
    }
}
