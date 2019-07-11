using CoreLib.Tools;
using Rss.Parsers.Rss;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Net;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Windows.UI.Xaml;
using Windows.Web.Syndication;

namespace CoreLib.Models
{
    /// <summary>
    /// 订阅源下的文章Model
    /// </summary>
    public class Feed:RssSchema,INotifyPropertyChanged
    {
        /// <summary>
        /// 标准化日期输出
        /// </summary>
        public string Date { get; set; }
        private Visibility _imgVisibility;
        /// <summary>
        /// 用于控制图片的显示
        /// </summary>
        public Visibility ImgVisibility
        {
            get { return _imgVisibility; }
            set { _imgVisibility = value;OnPropertyChanged(); }
        }
        private Visibility _tagVisibility;
        /// <summary>
        /// 用于控制标签的显示
        /// </summary>
        public Visibility TagVisibility
        {
            get { return _tagVisibility; }
            set { _tagVisibility = value; OnPropertyChanged(); }
        }
        public Feed() { }
        public Feed(RssSchema schema)
        {
            Title = schema.Title ?? "";
            Summary = schema.Summary ?? "";
            Content = schema.Content ?? "";
            string theme = AppTools.GetLocalSetting(Enums.AppSettings.Theme, "Light").ToLower();
            ImageUrl = schema.ImageUrl;
            if (string.IsNullOrEmpty(ImageUrl))
            {
                ImgVisibility = Visibility.Collapsed;
            }
            else if (ImageUrl.StartsWith("//"))
            {
                ImageUrl = "http:" + ImageUrl;
            }
            ExtraImageUrl = schema.ExtraImageUrl??"";
            MediaUrl = schema.MediaUrl??"";
            InternalID = schema.InternalID;
            FeedUrl = schema.FeedUrl;
            Author = schema.Author??AppTools.GetReswLanguage("App_NoAuthor");
            Date = schema.PublishDate.ToString(AppTools.GetReswLanguage("App_DateFormat"));
            Categories = schema.Categories;
            Encoding = schema.Encoding;
            if(Categories==null || Categories.Count() == 0)
            {
                TagVisibility = Visibility.Collapsed;
            }
        }
        public event PropertyChangedEventHandler PropertyChanged;
        public void OnPropertyChanged([CallerMemberName]string propertyName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public override bool Equals(object obj)
        {
            return obj is Feed feed &&
                   FeedUrl == feed.FeedUrl;
        }

        public override int GetHashCode()
        {
            return 884517729 + EqualityComparer<string>.Default.GetHashCode(Date);
        }
    }
}
