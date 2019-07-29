using SharpDX.WIC;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Net;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Graphics.Imaging;
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
    public sealed partial class HoldImage : UserControl, INotifyPropertyChanged
    {


        public string Source
        {
            get { return (string)GetValue(SourceProperty); }
            set { SetValue(SourceProperty, value); }
        }

        // Using a DependencyProperty as the backing store for Source.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty SourceProperty =
            DependencyProperty.Register("Source", typeof(string), typeof(HoldImage), new PropertyMetadata(null,new PropertyChangedCallback(SourceChanged)));

        private async static void SourceChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if(e.NewValue!=null && e.NewValue is string url)
            {
                url = WebUtility.HtmlDecode(url);
                var c = d as HoldImage;
                var uri = new Uri(url);
                HttpWebRequest myrequest = (HttpWebRequest)WebRequest.Create(url);
                myrequest.Referer = $"http://{uri.Host}";
                myrequest.Headers.Add(HttpRequestHeader.UserAgent, "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/77.0.3851.0 Safari/537.36 Edg/77.0.223.0");
                WebResponse myresponse = await myrequest.GetResponseAsync();
                
                var bitmap = new BitmapImage();
                using (var imgstream = myresponse.GetResponseStream())
                using (var stream = new MemoryStream())
                {
                    await imgstream.CopyToAsync(stream);
                    stream.Position = 0;
                    await bitmap.SetSourceAsync(stream.AsRandomAccessStream());
                    c.ImageBlock.Source = bitmap;
                }
                
            }
        }

        private string _imageLink;
        /// <summary>
        /// 图片链接
        /// </summary>
        public string ImageLink
        {
            get => _imageLink;
            set { _imageLink = value; OnPropertyChanged(); }
        }


        public Stretch Stretch
        {
            get { return (Stretch)GetValue(StretchProperty); }
            set { SetValue(StretchProperty, value); }
        }

        // Using a DependencyProperty as the backing store for Stretch.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty StretchProperty =
            DependencyProperty.Register("Stretch", typeof(Stretch), typeof(HoldImage), new PropertyMetadata(Stretch.UniformToFill));


        private BitmapImage _holderImage;
        /// <summary>
        /// 占位图片
        /// </summary>
        public BitmapImage HolderImage
        {
            get => _holderImage;
            set { _holderImage = value; OnPropertyChanged(); }
        }
        /// <summary>
        /// ImageEx的二次封装
        /// </summary>
        public HoldImage()
        {
            this.InitializeComponent();
            bool isDark = App.Current.RequestedTheme == ApplicationTheme.Dark;
            if (isDark)
            {
                HolderImage = new BitmapImage(new Uri("ms-appx:///Assets/imgHolder_dark.png"));
            }
            else
            {
                HolderImage = new BitmapImage(new Uri("ms-appx:///Assets/imgHolder_light.png"));
            }
        }
        public event PropertyChangedEventHandler PropertyChanged;
        public void OnPropertyChanged([CallerMemberName]string propertyName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
