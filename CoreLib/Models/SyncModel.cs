using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CoreLib.Models
{
    public class SyncModel
    {
        public string Name { get; set; }
        public string Time { get; set; }
        public SyncModel()
        {
            
        }
        public SyncModel(string name,string time)
        {
            Name = name;
            Time = time;
        }
    }
}
