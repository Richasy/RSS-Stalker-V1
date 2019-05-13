using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace CoreLib.Models
{
    public class Outline
    {
        ///<summary>
        /// Text of the XML file (required)
        ///</summary>
        public string Text { get; set; }

        ///<summary>
        /// true / false
        ///</summary>
        public string IsComment { get; set; }

        ///<summary>
        /// true / false
        ///</summary>
        public string IsBreakpoint { get; set; }

        ///<summary>
        /// outline node was created
        ///</summary>
        public DateTime? Created { get; set; } = null;

        ///<summary>
        /// Categories
        ///</summary>
        public List<string> Category { get; set; } = new List<string>();

        ///<summary>
        /// Description
        ///</summary>
        public string Description { get; set; }

        ///<summary>
        /// HTML URL
        ///</summary>
        public string HTMLUrl { get; set; }

        ///<summary>
        /// Language
        ///</summary>
        public string Language { get; set; }

        ///<summary>
        /// Title 
        ///</summary>
        public string Title { get; set; }

        ///<summary>
        /// Type (rss/atom)
        ///</summary>
        public string Type { get; set; }

        ///<summary>
        /// Version of RSS. 
        /// RSS1 for RSS1.0. RSS for 0.91, 0.92 or 2.0.
        ///</summary>
        public string Version { get; set; }

        ///<summary>
        /// URL of the XML file
        ///</summary>
        public string XMLUrl { get; set; }

        ///<summary>
        /// Outline list
        ///</summary>
        public List<Outline> Outlines { get; set; } = new List<Outline>();

        ///<summary>
        /// Constructor
        ///</summary>
        public Outline()
        {

        }

        public Outline(Channel channel)
        {
            Type = "rss";
            Text = channel.Description;
            Title = channel.Name;
            XMLUrl = channel.Link;
            HTMLUrl = channel.SourceUrl;
        }

        ///<summary>
        /// Constructor
        ///</summary>
        /// <param name="element">element of Head</param>
        public Outline(XmlElement element)
        {
            Text = element.GetAttribute("text");
            IsComment = element.GetAttribute("isComment");
            IsBreakpoint = element.GetAttribute("isBreakpoint");
            Created = GetDateTimeAttribute(element, "created");
            Category = GetCategoriesAtrribute(element, "category");
            Description = element.GetAttribute("description");
            HTMLUrl = element.GetAttribute("htmlUrl");
            Language = element.GetAttribute("language");
            Title = element.GetAttribute("title");
            Type = element.GetAttribute("type");
            Version = element.GetAttribute("version");
            XMLUrl = element.GetAttribute("xmlUrl");

            if (element.HasChildNodes)
            {
                foreach (XmlNode child in element.ChildNodes)
                {
                    if (child.Name.Equals("outline", StringComparison.CurrentCultureIgnoreCase))
                    {
                        Outlines.Add(new Outline((XmlElement)child));
                    }
                }
            }
        }

        private DateTime? GetDateTimeAttribute(XmlElement element, string name)
        {
            string dt = element.GetAttribute(name);

            try
            {
                return DateTime.Parse(dt);
            }
            catch
            {
                return null;
            }
        }

        private List<string> GetCategoriesAtrribute(XmlElement element, string name)
        {
            List<string> list = new List<string>();
            var items = element.GetAttribute(name).Split(',');
            foreach (var item in items)
            {
                list.Add(item.Trim());
            }
            return list;
        }

        public override string ToString()
        {
            StringBuilder buf = new StringBuilder();
            buf.Append("<outline");
            buf.Append(GetAtrributeString("text", Text));
            buf.Append(GetAtrributeString("isComment", IsComment));
            buf.Append(GetAtrributeString("isBreakpoint", IsBreakpoint));
            buf.Append(GetAtrributeString("created", Created));
            buf.Append(GetAtrributeString("category", Category));
            buf.Append(GetAtrributeString("description", Description));
            buf.Append(GetAtrributeString("htmlUrl", HTMLUrl));
            buf.Append(GetAtrributeString("language", Language));
            buf.Append(GetAtrributeString("title", Title));
            buf.Append(GetAtrributeString("type", Type));
            buf.Append(GetAtrributeString("version", Version));
            buf.Append(GetAtrributeString("xmlUrl", XMLUrl));

            if (Outlines.Count > 0)
            {
                buf.Append(">\r\n");
                foreach (Outline outline in Outlines)
                {
                    buf.Append(outline.ToString());
                }
                buf.Append("</outline>\r\n");
            }
            else
            {
                buf.Append(" />\r\n");
            }
            return buf.ToString();
        }

        private string GetAtrributeString(string name, string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return string.Empty;
            }
            else
            {
                return $" {name}=\"{value}\"";
            }
        }

        private string GetAtrributeString(string name, DateTime? value)
        {
            if (value == null)
            {
                return string.Empty;
            }
            else
            {
                return $" {name}=\"{value?.ToString("R")}\"";
            }
        }

        private string GetAtrributeString(string name, List<string> value)
        {
            if (value.Count == 0)
            {
                return string.Empty;
            }

            StringBuilder buf = new StringBuilder();
            foreach (var item in value)
            {
                buf.Append(item);
                buf.Append(",");
            }

            return $" {name}=\"{buf.Remove(buf.Length - 1, 1).ToString()}\"";
        }
    }
}
