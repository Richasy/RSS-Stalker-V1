using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using Windows.Web.Syndication;

namespace CoreLib.Models
{
    /// <summary>
    /// 软件的频道Model，即订阅源
    /// </summary>
    public class Channel:INotifyPropertyChanged
    {
        private string _name;
        private string _description;
        /// <summary>
        /// 频道名称
        /// </summary>
        public string Name
        {
            get { return _name; }
            set { _name = value; OnPropertyChanged(); }
        }
        /// <summary>
        /// RSS数据源链接
        /// </summary>
        public string Link { get; set; }
        /// <summary>
        /// 频道描述
        /// </summary>
        public string Description
        {
            get { return _description; }
            set { _description = value; OnPropertyChanged(); }
        }
        /// <summary>
        /// 频道对应的网页链接
        /// </summary>
        public string SourceUrl { get; set; }
        public string Id { get; set; }
        public Channel()
        {
            Id = Guid.NewGuid().ToString("N");
        }
        public Channel(Outline outline) : this()
        {
            if (!string.IsNullOrEmpty(outline.XMLUrl))
            {
                Name = outline.Title;
                Description = outline.Text;
                Link = outline.XMLUrl;
                SourceUrl = outline.HTMLUrl;
            }
        }
        public Channel(SyndicationFeed feed,string url):this()
        {
            Name = feed.Title.Text;
            Link = url;
            Description = feed.Subtitle.Text;
            var link = feed.Links.FirstOrDefault();
            if (link != null)
            {
                SourceUrl = link.Uri.ToString();
            }
        }
        public Channel(FeedlyResult feedly):this()
        {
            Name = feedly.Title;
            Link = feedly.FellowLink;
            Description = feedly.Description;
            SourceUrl = feedly.SourceLink;
        }

        public override bool Equals(object obj)
        {
            return obj is Channel channel &&
                   (Id == channel.Id || Link==channel.Link);
        }

        public override int GetHashCode()
        {
            return 924860401 + EqualityComparer<string>.Default.GetHashCode(Id);
        }

        public event PropertyChangedEventHandler PropertyChanged;
        public void OnPropertyChanged([CallerMemberName]string propertyName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
