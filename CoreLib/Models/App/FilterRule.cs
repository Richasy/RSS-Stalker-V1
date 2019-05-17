using CoreLib.Enums;
using CoreLib.Tools;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CoreLib.Models.App
{
    /// <summary>
    /// 筛选条件
    /// </summary>
    public class FilterRule
    {
        public string RuleName { get; set; }
        public FilterRuleType Type { get; set; }
        public string Content { get; set; }

        public override string ToString()
        {
            return RuleName+": "+Content;
        }
        public FilterRule()
        {
            RuleName = "";
            Content = "";
        }
        public FilterRule(FilterRuleType type):base()
        {
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
    }
}
