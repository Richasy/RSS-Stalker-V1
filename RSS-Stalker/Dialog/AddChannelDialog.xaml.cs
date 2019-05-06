using RSS_Stalker.Controls;
using RSS_Stalker.Models;
using RSS_Stalker.Tools;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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

// https://go.microsoft.com/fwlink/?LinkId=234238 上介绍了“内容对话框”项模板

namespace RSS_Stalker.Dialog
{
    public sealed partial class AddChannelDialog : ContentDialog
    {
        private Channel _sourceChannel = null;
        private ObservableCollection<FeedlyResult> FeedlyResults = new ObservableCollection<FeedlyResult>();
        public AddChannelDialog()
        {
            this.InitializeComponent();
            Title = AppTools.GetReswLanguage("Tip_AddChannel");
            PrimaryButtonText = AppTools.GetReswLanguage("Tip_Confirm");
            SecondaryButtonText = AppTools.GetReswLanguage("Tip_Cancel");
        }

        private async void ContentDialog_PrimaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
        {
            args.Cancel = true;
            if (_sourceChannel != null)
            {
                string showName = ChannelNameTextBox.Text;
                string showDescription = ChannelDescriptionTextBox.Text;
                if (!string.IsNullOrEmpty(showName))
                    _sourceChannel.Name = showName;
                if (!string.IsNullOrEmpty(showDescription))
                    _sourceChannel.Description = showDescription;
                var selectCategory = MainPage.Current.CategoryListView.SelectedItem as Category;
                if (selectCategory != null)
                {
                    if (selectCategory.Channels.Any(c => c.Link.ToLower() == _sourceChannel.Link))
                    {
                        new PopupToast(AppTools.GetReswLanguage("Tip_ChannelRepeat")).ShowPopup();
                        return;
                    }
                    else
                    {
                        IsPrimaryButtonEnabled = false;
                        PrimaryButtonText = AppTools.GetReswLanguage("Tip_Waiting");
                        selectCategory.Channels.Add(_sourceChannel);
                        await IOTools.UpdateCategory(selectCategory);
                        MainPage.Current.Channels.Add(_sourceChannel);
                        new PopupToast(AppTools.GetReswLanguage("Tip_AddChannelSuccess")).ShowPopup();
                        Hide();
                    }
                }
                else
                {
                    new PopupToast(AppTools.GetReswLanguage("Tip_NoCategorySelected")).ShowPopup();
                    return;
                }
            }
            else
            {
                new PopupToast(AppTools.GetReswLanguage("Tip_TryLinkFirst")).ShowPopup();
            }
        }

        private void ContentDialog_SecondaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
        {
        }

        private async void TryLinkButton_Click(object sender, RoutedEventArgs e)
        {
            var reg = new Regex(@"(https?|ftp|file)://[-A-Za-z0-9+&@#/%?=~_|!:,.;]+[-A-Za-z0-9+&@#/%=~_|]");
            string link = ChannelLinkTextBox.Text.Trim();
            if (string.IsNullOrEmpty(link))
            {
                new PopupToast(AppTools.GetReswLanguage("Tip_FieldEmpty")).ShowPopup();
            }
            else
            {
                TryLinkButton.IsEnabled = false;
                LoadingRing.IsActive = true;
                if (!reg.IsMatch(link))
                {
                    var results = await FeedlyResult.GetFeedlyResultFromText(link);
                    TryLinkButton.IsEnabled = true;
                    LoadingRing.IsActive = false;
                    if (results.Count > 0)
                    {
                        SearchResultContainer.Visibility = Visibility.Visible;
                        FeedlyResults.Clear();
                        foreach (var item in results)
                        {
                            FeedlyResults.Add(item);
                        }
                    }
                    else
                    {
                        SearchResultContainer.Visibility = Visibility.Collapsed;
                        FeedlyResults.Clear();
                        new PopupToast(AppTools.GetReswLanguage("Tip_NoData")).ShowPopup();
                    }
                }
                else
                {
                    var channel = await AppTools.GetChannelFromUrl(link);
                    if (channel != null && !string.IsNullOrEmpty(channel.Name))
                    {
                        _sourceChannel = channel;
                        LoadingRing.IsActive = false;
                        TryLinkButton.IsEnabled = true;
                        DetailContainer.Visibility = Visibility.Visible;
                        if (string.IsNullOrEmpty(ChannelNameTextBox.Text) && string.IsNullOrEmpty(ChannelDescriptionTextBox.Text))
                        {
                            ChannelNameTextBox.Text = channel.Name;
                            ChannelDescriptionTextBox.Text = channel.Description;
                        }
                    }
                }
            }
        }

        private void ChannelLinkTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            _sourceChannel = null;
            if (DetailContainer.Visibility == Visibility.Visible)
            {
                DetailContainer.Visibility = Visibility.Collapsed;
            }
            if (SearchResultContainer.Visibility == Visibility.Visible)
            {
                SearchResultContainer.Visibility = Visibility.Collapsed;
            }
        }

        private void SearchResultListView_ItemClick(object sender, ItemClickEventArgs e)
        {
            var item = e.ClickedItem as FeedlyResult;
            _sourceChannel = new Channel(item);
            DetailContainer.Visibility = Visibility.Visible;
            ChannelNameTextBox.Text = _sourceChannel.Name;
            ChannelDescriptionTextBox.Text = _sourceChannel.Description;
            FeedlyResults.Clear();
            SearchResultContainer.Visibility = Visibility.Collapsed;
        }
    }
}
