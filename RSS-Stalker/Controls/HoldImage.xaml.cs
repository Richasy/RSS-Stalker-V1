using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
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
        private string _imageLink;
        /// <summary>
        /// 图片链接
        /// </summary>
        public string ImageLink
        {
            get => _imageLink;
            set { _imageLink = value; OnPropertyChanged(); }
        }
        private BitmapImage _holderImage;
        /// <summary>
        /// 占位图片
        /// </summary>
        public BitmapImage HolderImage
        {
            get => _holderImage;
            set { _holderImage = value; OnPropertyChanged(); }
        }
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
