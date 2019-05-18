using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CoreLib.Models.App
{
    public class CustomPage
    {
        public string Name { get; set; }
        public string Icon { get; set; }
        public string Id { get; set; }
        public IList<Channel> Channels { get; set; }
        public IList<FilterItem> Rules { get; set; }
        public CustomPage()
        {
            Channels = new List<Channel>();
            Rules = new List<FilterItem>();
        }
        public override bool Equals(object obj)
        {
            return obj is CustomPage page &&
                   Id == page.Id;
        }

        public override int GetHashCode()
        {
            return 2108858624 + EqualityComparer<string>.Default.GetHashCode(Id);
        }
    }
}
