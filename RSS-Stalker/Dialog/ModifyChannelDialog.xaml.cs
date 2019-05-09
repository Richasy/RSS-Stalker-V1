using RSS_Stalker.Controls;
using CoreLib.Models;
using CoreLib.Tools;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text.RegularExpressions;
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
            string link = ChannelLinkTextBox.Text?.Trim();
            var reg = new Regex(@"(https?|ftp|file)://[-A-Za-z0-9+&@#/%?=~_|!:,.;]+[-A-Za-z0-9+&@#/%=~_|]");
            if (string.IsNullOrEmpty(name) || string.IsNullOrEmpty(des) || string.IsNullOrEmpty(link))
            {
                new PopupToast(AppTools.GetReswLanguage("Tip_FieldEmpty")).ShowPopup();
                return;
            }
            else if (!reg.IsMatch(link))
            {
                new PopupToast(AppTools.GetReswLanguage("Tip_FieldFormatError")).ShowPopup();
                return;
            }
            else
            {
                _sourceChannel.Name = name;
                _sourceChannel.Description = des;
                _sourceChannel.Link = link;
                var sourceCategory = MainPage.Current.CategoryListView.SelectedItem as Category;
                if (sourceCategory != null)
                {
                    IsPrimaryButtonEnabled = false;
                    PrimaryButtonText = AppTools.GetReswLanguage("Tip_Waiting");
                    foreach (var item in sourceCategory.Channels)
                    {
                        if (item.Id == _sourceChannel.Id)
                        {
                            item.Name = name;
                            item.Description = des;
                            item.Link = link;
                        }
                    }
                    await IOTools.UpdateCategory(sourceCategory);
                    if(MainPage.Current.MainFrame.Content is Pages.ChannelDetailPage)
                    {
                        if (Pages.ChannelDetailPage.Current._sourceData.Id == _sourceChannel.Id)
                        {
                            Pages.ChannelDetailPage.Current.ChannelNameTextBlock.Text = name;
                            Pages.ChannelDetailPage.Current.ChannelDescriptionTextBlock.Text = des;
                            Pages.ChannelDetailPage.Current._sourceData.Name = name;
                            Pages.ChannelDetailPage.Current._sourceData.Description = des;
                            Pages.ChannelDetailPage.Current._sourceData.Link = link;
                        }
                    }
                    new PopupToast(AppTools.GetReswLanguage("Tip_UpdateChannelSuccess")).ShowPopup();
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
