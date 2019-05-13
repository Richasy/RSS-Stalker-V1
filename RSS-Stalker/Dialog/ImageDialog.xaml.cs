using CoreLib.Tools;
using RSS_Stalker.Controls;
using RSS_Stalker.Tools;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.ApplicationModel.DataTransfer;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Storage;
using Windows.Storage.Streams;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Imaging;
using Windows.UI.Xaml.Navigation;

// https://go.microsoft.com/fwlink/?LinkId=234238 上介绍了“内容对话框”项模板

namespace RSS_Stalker.Dialog
{
    /// <summary>
    /// 图片显示对话框
    /// </summary>
    public sealed partial class ImageDialog : ContentDialog
    {
        private string ImageUrl = "";
        public ImageDialog(string imageLink)
        {
            this.InitializeComponent();
            LoadingRing.IsActive = true;
            ImageUrl = imageLink;
            var bitmap = new BitmapImage();
            bitmap.UriSource = new Uri(imageLink);
            ImageControl.Source = bitmap;
            Title = AppTools.GetReswLanguage("Tip_ImageView");
            PrimaryButtonText = AppTools.GetReswLanguage("Tip_Save");
            SecondaryButtonText = AppTools.GetReswLanguage("Tip_Copy");
            CloseButtonText = AppTools.GetReswLanguage("Tip_Cancel");
        }

        private async void ContentDialog_PrimaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
        {
            args.Cancel = true;
            IsPrimaryButtonEnabled = false;
            var file = await IOTools.GetSaveFile(".png", "Image", "PNG File");
            if (file != null)
            {
                var imageStream = await AppTools.GetImageStreamFromUrl(ImageUrl);
                await imageStream.CopyToAsync((await file.OpenAsync(FileAccessMode.ReadWrite)).AsStreamForWrite());
                new PopupToast(AppTools.GetReswLanguage("Tip_Saved")).ShowPopup();
                Hide();
            }
            else
            {
                IsPrimaryButtonEnabled = true;
            }
        }

        private async void ContentDialog_SecondaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
        {
            args.Cancel = true;
            IsSecondaryButtonEnabled = false;
            var file = await IOTools.CreateTempFile("temp.png");
            var imageStream = await AppTools.GetImageStreamFromUrl(ImageUrl);
            await imageStream.CopyToAsync((await file.OpenAsync(FileAccessMode.ReadWrite)).AsStreamForWrite());
            var datapackage = new DataPackage();
            datapackage.SetBitmap(RandomAccessStreamReference.CreateFromFile(file));
            Clipboard.SetContent(datapackage);
            new PopupToast(AppTools.GetReswLanguage("Tip_Copied")).ShowPopup();
            Hide();
        }

        private void ImageControl_ImageOpened(object sender, RoutedEventArgs e)
        {
            LoadingRing.IsActive = false;
        }
    }
}
