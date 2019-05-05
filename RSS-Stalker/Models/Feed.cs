using Microsoft.Toolkit.Parsers.Rss;
using RSS_Stalker.Tools;
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

namespace RSS_Stalker.Models
{
    public class Feed:RssSchema,INotifyPropertyChanged
    {
        public string Date { get; set; }
        private Visibility _imgVisibility;
        public Visibility ImgVisibility
        {
            get { return _imgVisibility; }
            set { _imgVisibility = value;OnPropertyChanged(); }
        }
        public Feed() { }
        public Feed(RssSchema schema)
        {
            Title = schema.Title;
            Summary = schema.Summary;
            Content = schema.Content;
            string theme = AppTools.GetLocalSetting(Enums.AppSettings.Theme, "Light").ToLower();
            ImageUrl = schema.ImageUrl??$"ms-appx:///Assets/imgHolder_{theme}.png";
            if (ImageUrl.StartsWith("//"))
            {
                ImageUrl = "http:" + ImageUrl;
            }
            ExtraImageUrl = schema.ExtraImageUrl??"";
            MediaUrl = schema.MediaUrl;
            InternalID = schema.InternalID;
            FeedUrl = schema.FeedUrl;
            Author = schema.Author??AppTools.GetReswLanguage("App_NoAuthor");
            Date = schema.PublishDate.ToString(AppTools.GetReswLanguage("App_DateFormat"));
            Categories = schema.Categories.ToList();
        }
        public event PropertyChangedEventHandler PropertyChanged;
        public void OnPropertyChanged([CallerMemberName]string propertyName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public override bool Equals(object obj)
        {
            return obj is Feed feed &&
                   Date == feed.InternalID;
        }

        public override int GetHashCode()
        {
            return 884517729 + EqualityComparer<string>.Default.GetHashCode(Date);
        }
    }
}
