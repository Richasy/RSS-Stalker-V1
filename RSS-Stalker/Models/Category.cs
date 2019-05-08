
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace RSS_Stalker.Models
{
    public class Category:INotifyPropertyChanged
    {
        private string _name;
        private string _icon;
        public string Name
        {
            get { return _name; }
            set { _name = value; OnPropertyChanged(); }
        }
        public string Icon
        {
            get { return _icon; }
            set { _icon = value; OnPropertyChanged(); }
        }
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
        public Category(Outline outline):this()
        {
            Name = outline.Title;
            Icon = "";
            Channels = new List<Channel>();
            if (outline.Outlines.Count > 0)
            {
                foreach (var item in outline.Outlines)
                {
                    var c = new Channel(item);
                    if (!string.IsNullOrEmpty(c.Name))
                    {
                        Channels.Add(c);
                    }
                }
            }
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

        public event PropertyChangedEventHandler PropertyChanged;
        public void OnPropertyChanged([CallerMemberName]string propertyName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
