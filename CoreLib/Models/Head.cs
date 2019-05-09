using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace CoreLib.Models
{
    public class Head
    {
        ///<summary>
        /// title
        ///</summary>
        public string Title { get; set; }

        ///<summary>
        /// Created date
        ///</summary>
        public DateTime? DateCreated { get; set; } = null;

        ///<summary>
        /// Modified date
        ///</summary>
        public DateTime? DateModified { get; set; } = null;

        ///<summary>
        /// ownerName
        ///</summary>
        public string OwnerName { get; set; }

        ///<summary>
        /// ownerEmail
        ///</summary>
        public string OwnerEmail { get; set; }

        ///<summary>
        /// ownerId
        ///</summary>
        public string OwnerId { get; set; }

        ///<summary>
        /// docs
        ///</summary>
        public string Docs { get; set; }

        ///<summary>
        /// expansionState
        ///</summary>
        public List<string> ExpansionState { get; set; } = new List<string>();

        ///<summary>
        /// vertScrollState
        ///</summary>
        public string VertScrollState { get; set; }

        ///<summary>
        /// windowTop
        ///</summary>
        public string WindowTop { get; set; }

        ///<summary>
        /// windowLeft
        ///</summary>
        public string WindowLeft { get; set; }

        ///<summary>
        /// windowBottom
        ///</summary>
        public string WindowBottom { get; set; }

        ///<summary>
        /// windowRight
        ///</summary>
        public string WindowRight { get; set; }

        ///<summary>
        /// Constructor
        ///</summary>
        public Head()
        {

        }

        ///<summary>
        /// Constructor
        ///</summary>
        /// <param name="element">element of Head</param>
        public Head(XmlElement element)
        {
            if (element.Name.Equals("head", StringComparison.CurrentCultureIgnoreCase))
            {
                foreach (XmlNode node in element.ChildNodes)
                {
                    Title = GetStringValue(node, "title", Title);
                    DateCreated = GetDateTimeValue(node, "dateCreated", DateCreated);
                    DateModified = GetDateTimeValue(node, "dateModified", DateModified);
                    OwnerName = GetStringValue(node, "ownerName", OwnerName);
                    OwnerEmail = GetStringValue(node, "ownerEmail", OwnerEmail);
                    OwnerId = GetStringValue(node, "ownerId", OwnerId);
                    Docs = GetStringValue(node, "docs", Docs);
                    ExpansionState = GetExpansionState(node, "expansionState", ExpansionState);
                    VertScrollState = GetStringValue(node, "vertScrollState", VertScrollState);
                    WindowTop = GetStringValue(node, "windowTop", WindowTop);
                    WindowLeft = GetStringValue(node, "windowLeft", WindowLeft);
                    WindowBottom = GetStringValue(node, "windowBottom", WindowBottom);
                    WindowRight = GetStringValue(node, "windowRight", WindowRight);
                }
            }
        }

        private string GetStringValue(XmlNode node, string name, string value)
        {
            if (node.Name.Equals(name, StringComparison.CurrentCultureIgnoreCase))
            {
                return node.InnerText;
            }
            else if (!node.Name.Equals(name, StringComparison.CurrentCultureIgnoreCase))
            {
                return value;
            }
            else
            {
                return string.Empty;
            }
        }

        private DateTime? GetDateTimeValue(XmlNode node, string name, DateTime? value)
        {
            if (node.Name.Equals(name, StringComparison.CurrentCultureIgnoreCase))
            {
                try
                {
                    return DateTime.Parse(node.InnerText);
                }
                catch
                {
                    return null;
                }
            }
            else if (!node.Name.Equals(name, StringComparison.CurrentCultureIgnoreCase))
            {
                return value;
            }
            else
            {
                return null;
            }
        }

        private List<string> GetExpansionState(XmlNode node, string name, List<string> value)
        {
            if (node.Name.Equals(name, StringComparison.CurrentCultureIgnoreCase))
            {
                List<string> list = new List<string>();
                var items = node.InnerText.Split(',');
                foreach (var item in items)
                {
                    list.Add(item.Trim());
                }
                return list;

            }
            else if (!node.Name.Equals(name, StringComparison.CurrentCultureIgnoreCase))
            {
                return value;
            }
            else
            {
                return new List<string>();
            }
        }

        public override string ToString()
        {
            StringBuilder buf = new StringBuilder();
            buf.Append("<head>\r\n");
            buf.Append(GetNodeString("title", Title));
            buf.Append(GetNodeString("dateCreated", DateCreated));
            buf.Append(GetNodeString("dateModified", DateModified));
            buf.Append(GetNodeString("ownerName", OwnerName));
            buf.Append(GetNodeString("ownerEmail", OwnerEmail));
            buf.Append(GetNodeString("ownerId", OwnerId));
            buf.Append(GetNodeString("docs", Docs));
            buf.Append(GetNodeString("expansionState", ExpansionState));
            buf.Append(GetNodeString("vertScrollState", VertScrollState));
            buf.Append(GetNodeString("windowTop", WindowTop));
            buf.Append(GetNodeString("windowLeft", WindowLeft));
            buf.Append(GetNodeString("windowBottom", WindowBottom));
            buf.Append(GetNodeString("windowRight", WindowRight));
            buf.Append("</head>\r\n");
            return buf.ToString();
        }

        private string GetNodeString(string name, string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return string.Empty;
            }
            else
            {
                return $"<{name}>{value}</{name}>\r\n";
            }
        }
        private string GetNodeString(string name, DateTime? value)
        {
            if (value == null)
            {
                return string.Empty;
            }
            else
            {
                return $"<{name}>{value?.ToString("R")}</{name}>\r\n";
            }
        }

        private string GetNodeString(string name, List<string> value)
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

            return $"<{name}>{buf.Remove(buf.Length - 1, 1).ToString()}</{name}>\r\n";
        }
    }
}
