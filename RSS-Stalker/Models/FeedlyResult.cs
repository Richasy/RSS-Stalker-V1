using RSS_Stalker.Tools;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace RSS_Stalker.Models
{
    public class FeedlyResult
    {
        public string Id { get; set; }
        public string CoverUrl { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public string FellowLink { get; set; }
        public string SourceLink { get; set; }
        public FeedlyResult()
        {

        }
        public FeedlyResult(WebFeedlyResultItem item)
        {
            Id = item.id;
            CoverUrl = item.iconUrl;
            Title = item.title;
            Description = item.description;
            FellowLink = item.feedId;
            if (FellowLink.StartsWith("feed/"))
            {
                FellowLink = FellowLink.Substring(5);
            }
            SourceLink = item.website;
        }
        public static async Task<List<FeedlyResult>> GetFeedlyResultFromText(string text)
        {
            string language = AppTools.GetLocalSetting(Enums.AppSettings.Language, "en_US").ToLower();
            if (language.Contains("_"))
            {
                language = language.Split("_")[0];
            }
            text = WebUtility.UrlEncode(text);
            double time = AppTools.DateToTimeStamp(DateTime.Now.ToLocalTime());
            string url = $"https://feedly.com/v3/search/feeds?q={text}&n=50&fullTerm=false&organic=true&promoted=true&locale={language}&useV2=true&ck={time}&ct=feedly.desktop&cv=31.0.336";
            var data = await AppTools.GetEntityFromUrl<WebFeedlyResult>(url);
            var results = new List<FeedlyResult>();
            if (data != null && data.results!=null && data.results.Length>0)
            {
                foreach (var item in data.results)
                {
                    if(item.state!= "dormant")
                        results.Add(new FeedlyResult(item));
                }
            }
            return results;
        }
    }

    public class WebFeedlyResult
    {
        public WebFeedlyResultItem[] results { get; set; }
        public string queryType { get; set; }
        public string[] related { get; set; }
        public string scheme { get; set; }
    }

    public class WebFeedlyResultItem
    {
        public string feedId { get; set; }
        public long lastUpdated { get; set; }
        public float score { get; set; }
        public float coverage { get; set; }
        public float averageReadTime { get; set; }
        public float coverageScore { get; set; }
        public int estimatedEngagement { get; set; }
        public int totalTagCount { get; set; }
        public string websiteTitle { get; set; }
        public string id { get; set; }
        public string title { get; set; }
        public int subscribers { get; set; }
        public long updated { get; set; }
        public float velocity { get; set; }
        public string website { get; set; }
        public string[] topics { get; set; }
        public bool partial { get; set; }
        public string coverUrl { get; set; }
        public string iconUrl { get; set; }
        public string visualUrl { get; set; }
        public string language { get; set; }
        public string contentType { get; set; }
        public string description { get; set; }
        public string coverColor { get; set; }
        public string[] deliciousTags { get; set; }
        public string state { get; set; }
    }

}
