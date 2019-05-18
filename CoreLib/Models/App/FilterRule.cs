using CoreLib.Enums;
using CoreLib.Tools;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace CoreLib.Models.App
{
    public class FilterRule
    {
        public string RuleName { get; set; }
        public FilterRuleType Type { get; set; }
        public override string ToString()
        {
            return RuleName;
        }
        public FilterRule()
        {
            RuleName = "";
        }
        public FilterRule(FilterRuleType type) : base()
        {
            Type = type;
            switch (type)
            {
                case FilterRuleType.Filter:
                    RuleName = AppTools.GetReswLanguage("Tip_RuleFilter");
                    break;
                case FilterRuleType.FilterOut:
                    RuleName = AppTools.GetReswLanguage("Tip_RuleFilterOut");
                    break;
                case FilterRuleType.TotalLimit:
                    RuleName = AppTools.GetReswLanguage("Tip_TotalLimit");
                    break;
                case FilterRuleType.SingleLimit:
                    RuleName = AppTools.GetReswLanguage("Tip_SingleLimit");
                    break;
                default:
                    break;
            }
        }
        public static List<FilterRule> GetRules()
        {
            var list = new List<FilterRule>
            {
                new FilterRule(FilterRuleType.Filter),
                new FilterRule(FilterRuleType.FilterOut),
                new FilterRule(FilterRuleType.TotalLimit),
                new FilterRule(FilterRuleType.SingleLimit),
            };
            return list;
        }

        public override bool Equals(object obj)
        {
            return obj is FilterRule rule &&
                   Type == rule.Type;
        }

        public override int GetHashCode()
        {
            return 2049151605 + Type.GetHashCode();
        }
    }
    /// <summary>
    /// 筛选条件
    /// </summary>
    public class FilterItem
    {
        //private FilterRule _rule;
        public FilterRule Rule
        {
            get;set;
        } 
        public string Content { get; set; }

        public override string ToString()
        {
            return Rule.RuleName+": "+Content;
        }
    }
}
