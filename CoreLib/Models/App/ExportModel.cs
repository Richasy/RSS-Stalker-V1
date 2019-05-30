using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CoreLib.Models.App
{
    /// <summary>
    /// 本地收藏列表、待读列表和推送列表的导出
    /// </summary>
    public class ExportModel
    {
        public List<Feed> Todo { get; set; }
        public List<Feed> Star { get; set; }
        public List<Channel> Toast { get; set; }
        public List<CustomPage> Pages { get; set; }
        public List<string> Reads { get; set; }
        public IEnumerable<Channel> Readable { get; set; }
    }
}
