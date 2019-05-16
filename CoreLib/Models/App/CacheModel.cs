using Rss.Parsers.Rss;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CoreLib.Models.App
{
    public class CacheModel
    {
        public Channel Channel { get; set; }
        public List<RssSchema> Feeds { get; set; }
    }
}
