using CoreLib.Tools;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace RSS_Stalker.Tools
{
    public class TranslateTools
    {
        private static string _baseUrl = "http://api.fanyi.baidu.com/api/trans/vip/translate";
        public async static Task<string> Translate(string input, string appId, string key, string from = "zh", string to = "en")
        {
            string salt = "1435660288";
            string tempSign = appId + input + salt + key;
            MD5 md5 = MD5.Create();
            byte[] tempSignBytes = md5.ComputeHash(Encoding.UTF8.GetBytes(tempSign));
            string sign = "";
            for (int i = 0; i < tempSignBytes.Length; i++)
            {
                sign = sign + tempSignBytes[i].ToString("x2");
            }
            string query = $"q={WebUtility.UrlEncode(input)}&from={from}&to={to}&appid={appId}&salt={salt}&sign={sign}";
            string url = $"{_baseUrl}?{query}";
            var translate = await AppTools.PostAsyncData(_baseUrl, query);
            var obj = JObject.Parse(translate);
            string result = "";
            foreach (var trans_result in obj["trans_result"])
                result += trans_result?["dst"].ToString() + "\r\n";
            return result.Trim();
        }
    }
    public class BaiduTranslateResult
    {
        public string src { get; set; }
        public string dst { get; set; }
        public string from { get; set; }
        public string to { get; set; }
    }
}
