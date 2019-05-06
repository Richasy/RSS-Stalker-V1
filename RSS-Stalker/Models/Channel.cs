using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Web.Syndication;

namespace RSS_Stalker.Models
{
    public class Channel
    {
        /// <summary>
        /// 频道名称
        /// </summary>
        public string Name { get; set; }
        /// <summary>
        /// RSS数据源链接
        /// </summary>
        public string Link { get; set; }
        /// <summary>
        /// 频道描述
        /// </summary>
        public string Description { get; set; }
        /// <summary>
        /// 频道原始链接
        /// </summary>
        public string SourceUrl { get; set; }
        public Channel()
        {
        }
        public Channel(SyndicationFeed feed,string url)
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
        public Channel(FeedlyResult feedly)
        {
            Name = feedly.Title;
            Link = feedly.FellowLink;
            Description = feedly.Description;
            SourceUrl = feedly.SourceLink;
        }

        public override bool Equals(object obj)
        {
            return obj is Channel channel &&
                   Link == channel.Link;
        }

        public override int GetHashCode()
        {
            return 924860401 + EqualityComparer<string>.Default.GetHashCode(Link);
        }
    }
}
