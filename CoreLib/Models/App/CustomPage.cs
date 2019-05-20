using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace CoreLib.Models.App
{
    public class CustomPage : INotifyPropertyChanged
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
        public event PropertyChangedEventHandler PropertyChanged;
        public void OnPropertyChanged([CallerMemberName]string propertyName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
