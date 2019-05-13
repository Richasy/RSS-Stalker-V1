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

// https://go.microsoft.com/fwlink/?LinkId=234238 上介绍了“内容对话框”项模板

namespace RSS_Stalker.Dialog
{
    public sealed partial class ConfirmDialog : ContentDialog
    {
        /// <summary>
        /// 带确认按钮的对话框
        /// </summary>
        public ConfirmDialog()
        {
            this.InitializeComponent();
            PrimaryButtonText = AppTools.GetReswLanguage("Tip_Confirm");
            SecondaryButtonText = AppTools.GetReswLanguage("Tip_Cancel");
        }
        public ConfirmDialog(string title,string content):this()
        {
            Title = title;
            ConfirmTextBlock.Text = content;
        }
        public ConfirmDialog(string title, string content,string primaryButtonText="",string secondaryButtonText="",string closeButtonText="") : this()
        {
            Title = title;
            ConfirmTextBlock.Text = content;
            PrimaryButtonText = primaryButtonText;
            SecondaryButtonText = secondaryButtonText;
            CloseButtonText = closeButtonText;
        }

        private void ContentDialog_PrimaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
        {
        }

        private void ContentDialog_SecondaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
        {
        }
    }
}
