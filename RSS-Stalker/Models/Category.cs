using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RSS_Stalker.Models
{
    public class Category
    {
        public string Name { get; set; }
        public string Icon { get; set; }
        public string Id { get; set; }
        public List<Channel> Channels { get; set; }
        public Category()
        {
            Id = Guid.NewGuid().ToString("N");
        }
        public Category(string name,string icon)
        {
            Id = Guid.NewGuid().ToString("N");
            Name = name;
            Icon = icon;
            Channels = new List<Channel>();
        }

        public override bool Equals(object obj)
        {
            return obj is Category category &&
                   Id == category.Id;
        }

        public override int GetHashCode()
        {
            return -737073652 + EqualityComparer<string>.Default.GetHashCode(Id);
        }
    }
}
