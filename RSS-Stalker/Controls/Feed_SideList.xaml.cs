
using Rss.Parsers.Rss;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

//https://go.microsoft.com/fwlink/?LinkId=234236 上介绍了“用户控件”项模板

namespace RSS_Stalker.Controls
{
    public sealed partial class Feed_SideList : UserControl
    {
        public Feed_SideList()
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
            DependencyProperty.Register("Data", typeof(RssSchema), typeof(Feed_SideList), new PropertyMetadata(null, new PropertyChangedCallback(Data_Changed)));

        private static void Data_Changed(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var data = e.NewValue as RssSchema;
            if (data != null)
            {
                var c = d as Feed_SideList;
                if (string.IsNullOrEmpty(data.ImageUrl))
                {
                    c.HoldImageControl.Visibility = Visibility.Collapsed;
                }
                else
                {
                    c.HoldImageControl.Visibility = Visibility.Visible;
                    c.HoldImageControl.ImageLink = data.ImageUrl.StartsWith("//") ? "http:" + data.ImageUrl : data.ImageUrl;
                }
                c.TitleBlock.Text = data.Title;
                c.SummaryBlock.Text = data.Summary;
                ToolTipService.SetToolTip(c.TitleBlock, data.Title);
            }
        }
    }
}
