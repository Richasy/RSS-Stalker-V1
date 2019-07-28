using CoreLib.Enums;
using CoreLib.Tools;
using Rss.Parsers.Rss;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.ApplicationModel.DataTransfer;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.System;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Imaging;
using Windows.UI.Xaml.Navigation;

//https://go.microsoft.com/fwlink/?LinkId=234236 上介绍了“用户控件”项模板

namespace RSS_Stalker.Controls
{
    public sealed partial class Feed_List : UserControl
    {
        public Feed_List()
        {
            this.InitializeComponent();
        }
        public RssSchema Data
        {
            get { return (RssSchema)GetValue(DataProperty); }
            set { SetValue(DataProperty, value); }
        }

        // Using a DependencyProperty as the backing store for Data.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty DataProperty =
            DependencyProperty.Register("Data", typeof(RssSchema), typeof(Feed_List), new PropertyMetadata(null, new PropertyChangedCallback(Data_Changed)));

        private static void Data_Changed(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var data = e.NewValue as RssSchema;
            if (data != null)
            {
                var c = d as Feed_List;
                c.TitleBlock.Text = data.Title;
                c.DateBlock.Text = data.PublishDate.ToString(AppTools.GetReswLanguage("App_DateFormat"));
                c.FavIconImage.Source = new BitmapImage(new Uri(AppTools.GetFavIcon(data.FeedUrl)));
                ToolTipService.SetToolTip(c.TitleBlock, data.Title);
            }
        }

        private async void OpenFeedButton_Click(object sender, RoutedEventArgs e)
        {
            if (Data != null)
            {
                if(!string.IsNullOrEmpty(Data.FeedUrl))
                    await Launcher.LaunchUriAsync(new Uri(Data.FeedUrl));
                else
                    new PopupToast(AppTools.GetReswLanguage("App_InvalidUrl"), AppTools.GetThemeSolidColorBrush(ColorType.ErrorColor)).ShowPopup();
            }
        }

        private void ShareFeedButton_Click(object sender, RoutedEventArgs e)
        {
            DataTransferManager dataTransferManager = DataTransferManager.GetForCurrentView();
            dataTransferManager.DataRequested += IndexPage_DataRequested;
            DataTransferManager.ShowShareUI();
        }
        private void IndexPage_DataRequested(DataTransferManager sender, DataRequestedEventArgs args)
        {
            //创建一个数据包
            DataPackage dataPackage = new DataPackage();
            //把要分享的链接放到数据包里
            dataPackage.SetHtmlFormat(HtmlFormatHelper.CreateHtmlFormat(Data.Content));
            dataPackage.SetWebLink(new Uri(Data.FeedUrl));
            //数据包的标题（内容和标题必须提供）
            dataPackage.Properties.Title = Data.Title;
            //数据包的描述
            dataPackage.Properties.Description = Data.Summary;
            //给dataRequest对象赋值
            DataRequest request = args.Request;
            request.Data = dataPackage;
        }
    }
}
