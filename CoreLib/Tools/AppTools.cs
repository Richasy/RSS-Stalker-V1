using CoreLib.Enums;
using Newtonsoft.Json;
using CoreLib.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Windows.ApplicationModel.Core;
using Windows.ApplicationModel.Resources;
using Windows.Storage;
using Windows.UI;
using Windows.UI.ViewManagement;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Media;
using Windows.Web.Syndication;
using static System.Net.Mime.MediaTypeNames;
using System.Diagnostics;
using System.Net;
using Newtonsoft.Json.Linq;
using Rss.Parsers.Rss;

namespace CoreLib.Tools
{
    public class AppTools
    {
        /// <summary>
        /// 写入本地设置
        /// </summary>
        /// <param name="key">设置名</param>
        /// <param name="value">设置值</param>
        public static void WriteLocalSetting(AppSettings key, string value)
        {
            var localSetting = ApplicationData.Current.LocalSettings;
            var localcontainer = localSetting.CreateContainer("RSS", ApplicationDataCreateDisposition.Always);
            localcontainer.Values[key.ToString()] = value;
        }
        /// <summary>
        /// 读取本地设置
        /// </summary>
        /// <param name="key">设置名</param>
        /// <returns></returns>
        public static string GetLocalSetting(AppSettings key, string defaultValue)
        {
            var localSetting = ApplicationData.Current.LocalSettings;
            var localcontainer = localSetting.CreateContainer("RSS", ApplicationDataCreateDisposition.Always);
            bool isKeyExist = localcontainer.Values.ContainsKey(key.ToString());
            if (isKeyExist)
            {
                return localcontainer.Values[key.ToString()].ToString();
            }
            else
            {
                WriteLocalSetting(key, defaultValue);
                return defaultValue;
            }
        }
        /// <summary>
        /// 写入漫游设置
        /// </summary>
        /// <param name="key">设置名</param>
        /// <param name="value">设置值</param>
        public static void WriteRoamingSetting(AppSettings key, string value)
        {
            var roamingSetting = ApplicationData.Current.RoamingSettings;
            var roamingcontainer = roamingSetting.CreateContainer("RSS", ApplicationDataCreateDisposition.Always);
            roamingcontainer.Values[key.ToString()] = value;
        }
        /// <summary>
        /// 读取漫游设置
        /// </summary>
        /// <param name="key">设置名</param>
        /// <returns></returns>
        public static string GetRoamingSetting(AppSettings key,string defaultValue)
        {
            var roamingSetting = ApplicationData.Current.RoamingSettings;
            var roamingcontainer = roamingSetting.CreateContainer("RSS", ApplicationDataCreateDisposition.Always);
            bool isKeyExist = roamingcontainer.Values.ContainsKey(key.ToString());
            if (isKeyExist)
            {
                return roamingcontainer.Values[key.ToString()].ToString();
            }
            else
            {
                WriteRoamingSetting(key, defaultValue);
                return defaultValue;
            }
        }

        /// <summary>
        /// 获取Unix时间戳
        /// </summary>
        /// <returns></returns>
        public static int DateToTimeStamp(DateTime date)
        {
            TimeSpan ts = date - new DateTime(1970, 1, 1, 0, 0, 0, 0);
            int seconds = Convert.ToInt32(ts.TotalSeconds);
            return seconds;
        }
        /// <summary>
        /// 转化Unix时间戳
        /// </summary>
        /// <returns></returns>
        public static DateTime TimeStampToDate(int seconds)
        {
            var date = new DateTime(1970, 1, 1, 0, 0, 0, 0).AddSeconds(seconds);
            return date;
        }
        /// <summary>
        /// 初始化标题栏
        /// </summary>
        public static void SetTitleBarColor()
        {
            var view = ApplicationView.GetForCurrentView();
            CoreApplication.GetCurrentView().TitleBar.ExtendViewIntoTitleBar = true;
            var Theme = GetLocalSetting(AppSettings.Theme, ApplicationTheme.Light.ToString());
            if (Theme == ApplicationTheme.Dark.ToString())
            {
                // active
                view.TitleBar.BackgroundColor = Colors.Transparent;
                view.TitleBar.ForegroundColor = Colors.White;

                // inactive
                view.TitleBar.InactiveBackgroundColor = Colors.Transparent;
                view.TitleBar.InactiveForegroundColor = Colors.Gray;
                // button
                view.TitleBar.ButtonBackgroundColor = Colors.Transparent;
                view.TitleBar.ButtonForegroundColor = Colors.White;

                view.TitleBar.ButtonHoverBackgroundColor = Color.FromArgb(255, 33, 42, 67);
                view.TitleBar.ButtonHoverForegroundColor = Colors.White;

                view.TitleBar.ButtonPressedBackgroundColor = Color.FromArgb(255, 255, 86, 86);
                view.TitleBar.ButtonPressedForegroundColor = Colors.White;

                view.TitleBar.ButtonInactiveBackgroundColor = Colors.Transparent;
                view.TitleBar.ButtonInactiveForegroundColor = Colors.Gray;
            }
            else
            {
                // active
                view.TitleBar.BackgroundColor = Colors.Transparent;
                view.TitleBar.ForegroundColor = Colors.Black;

                // inactive
                view.TitleBar.InactiveBackgroundColor = Colors.Transparent;
                view.TitleBar.InactiveForegroundColor = Colors.Gray;
                // button
                view.TitleBar.ButtonBackgroundColor = Colors.Transparent;
                view.TitleBar.ButtonForegroundColor = Colors.DarkGray;

                view.TitleBar.ButtonHoverBackgroundColor = Colors.LightGray;
                view.TitleBar.ButtonHoverForegroundColor = Colors.DarkGray;

                view.TitleBar.ButtonPressedBackgroundColor = Colors.DarkGray;
                view.TitleBar.ButtonPressedForegroundColor = Colors.White;

                view.TitleBar.ButtonInactiveBackgroundColor = Colors.Transparent;
                view.TitleBar.ButtonInactiveForegroundColor = Colors.Gray;
            }
        }

        /// <summary>
        /// 创建动态卡片所需的布局
        /// </summary>
        /// <param name="name">笔记名</param>
        /// <param name="markdown">笔记内容</param>
        /// <returns></returns>
        public async static Task<string> CreateAdaptiveJson(Feed feed)
        {
            var jsonFile = await StorageFile.GetFileFromApplicationUriAsync(new Uri("ms-appx:///Card.json"));
            string json = await FileIO.ReadTextAsync(jsonFile);
            string imageLink = feed.ImageUrl;
            json = json.Replace("$IMAGE$", imageLink);
            json = json.Replace("$TITLE$", feed.Title);
            json = json.Replace("$CONTENT$", feed.Summary);
            return json;
        }

        /// <summary>
        /// 获取当前指定的父控件
        /// </summary>
        /// <typeparam name="T">转换类型</typeparam>
        /// <param name="obj">控件</param>
        /// <param name="name">父控件名</param>
        /// <returns></returns>
        public static T GetParentObject<T>(DependencyObject obj, string name) where T : FrameworkElement
        {
            DependencyObject parent = VisualTreeHelper.GetParent(obj);

            while (parent != null)
            {
                if (parent is T && (((T)parent).Name == name | string.IsNullOrEmpty(name)))
                {
                    return (T)parent;
                }

                parent = VisualTreeHelper.GetParent(parent);
            }

            return null;
        }
        /// <summary>
        /// 获取当前控件的指定子控件
        /// </summary>
        /// <typeparam name="T">控件类型</typeparam>
        /// <param name="obj">父控件</param>
        /// <param name="name">子控件名</param>
        /// <returns></returns>
        public static T GetChildObject<T>(DependencyObject obj, string name) where T : FrameworkElement
        {
            DependencyObject child = null;
            T grandChild = null;

            for (int i = 0; i <= VisualTreeHelper.GetChildrenCount(obj) - 1; i++)
            {
                child = VisualTreeHelper.GetChild(obj, i);

                if (child is T && (((T)child).Name == name | string.IsNullOrEmpty(name)))
                {
                    return (T)child;
                }
                else
                {
                    grandChild = GetChildObject<T>(child, name);
                }
                if (grandChild != null)
                {
                    return grandChild;
                }
            }
            return null;
        }

        /// <summary>
        /// 标准化字符串，去掉空格，全部小写
        /// </summary>
        /// <param name="str">字符串</param>
        /// <returns></returns>
        public static string NormalString(string str)
        {
            str = str.ToLower();
            var reg = new Regex(@"\s", RegexOptions.IgnoreCase);
            str = reg.Replace(str, "");
            return str;
        }

        /// <summary>
        /// 获取预先定义的线性画笔资源
        /// </summary>
        /// <param name="key">键</param>
        /// <returns></returns>
        public static SolidColorBrush GetThemeSolidColorBrush(ColorType key)
        {
            return (SolidColorBrush)Windows.UI.Xaml.Application.Current.Resources[key.ToString()];
        }

        /// <summary>
        /// 获取网络图片数据流
        /// </summary>
        /// <param name="url">图片地址</param>
        /// <returns></returns>
        public static async Task<Stream> GetImageStreamFromUrl(string url)
        {
            var client = new HttpClient();
            using (client)
            {
                return await client.GetStreamAsync(url);
            }
        }
        /// <summary>
        /// 从URL获取文本
        /// </summary>
        /// <param name="url">地址</param>
        /// <returns></returns>
        public static async Task<string> GetTextFromUrl(string url)
        {
            try
            {
                using (var client = new HttpClient())
                {
                    return await client.GetStringAsync(url);
                }
            }
            catch (Exception)
            {
                return null;
            }
            
        }
        /// <summary>
        /// 从URL获取实体类
        /// </summary>
        /// <typeparam name="T">类型</typeparam>
        /// <param name="url">地址</param>
        /// <returns></returns>
        public static async Task<T> GetEntityFromUrl<T>(string url)
        {
            string text = await GetTextFromUrl(url);
            if (!string.IsNullOrEmpty(text))
            {
                try
                {
                    var result = JsonConvert.DeserializeObject<T>(text);
                    return result;
                }
                catch (Exception)
                {
                }
            }
            return default(T);
        }
        /// <summary>
        /// 从URL获取解析后的Channel的信息
        /// </summary>
        /// <param name="url">地址</param>
        /// <returns></returns>
        public static async Task<Channel> GetChannelFromUrl(string url)
        {
            var client = new SyndicationClient();
            client.Timeout = 15000;
            var feed = new SyndicationFeed();
            client.SetRequestHeader("User-Agent", "Mozilla/5.0 (compatible; MSIE 10.0; Windows NT 6.2; WOW64; Trident/6.0)");
            try
            {
                feed = await client.RetrieveFeedAsync(new Uri(url));
                if (feed != null)
                {
                    return new Channel(feed, url);
                }
                
            }
            catch (Exception)
            {
                
            }
            return null;
        }
        /// <summary>
        /// 从URL获取解析后的Item的信息
        /// </summary>
        /// <param name="url">地址</param>
        /// <returns></returns>
        public static async Task<List<RssSchema>> GetSchemalFromUrl(string url)
        {
            string feed = null;

            using (var client = new HttpClient())
            {
                try
                {
                    feed = await client.GetStringAsync(url);
                }
                catch { }
            }
            var list = new List<RssSchema>();
            if (feed != null)
            {
                try
                {
                    var parser = new RssParser();
                    var rss = parser.Parse(feed);
                    foreach (var item in rss)
                    {
                        list.Add(item);
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine(ex.Message);
                }

            }
            return list;
        }
        /// <summary>
        /// 从URL获取解析后的文章的信息
        /// </summary>
        /// <param name="url">地址</param>
        /// <returns></returns>
        public static async Task<List<Feed>> GetFeedsFromUrl(string url)
        {
            string feed = null;

            using (var client = new HttpClient())
            {
                try
                {
                    feed = await client.GetStringAsync(url);
                }
                catch { }
            }
            var list = new List<Feed>();
            if (feed != null)
            {
                try
                {
                    var parser = new RssParser();
                    var rss = parser.Parse(feed);

                    foreach (var item in rss)
                    {
                        list.Add(new Feed(item));
                    }
                }
                catch (Exception)
                {

                }
                
            }
            return list;
        }
        /// <summary>
        /// 根据语言选项选择对应语言的语句
        /// </summary>
        /// <param name="name">键值</param>
        /// <returns></returns>
        public static string GetReswLanguage(string name)
        {
            var loader = ResourceLoader.GetForCurrentView();
            var language = loader.GetString(name);
            language = language.Replace("\\n", "\n");
            return language;
        }
        /// <summary>
        /// 图标列表装载
        /// </summary>
        /// <returns></returns>
        public static List<string> GetIcons()
        {
            var list = new List<string>
            {
                "","","","","","","","","","","","","","","","","","","","","","",
                "","","","","","","","","","","","","","","","","","","","","","",
                "","","","","","","","","","","","","","","","","","","","","","",
                "","","","","","","","","","","","","","","","","","","","","","",
                "","","","","","","","","","","","","","","","","","","","","","",
                "","","","","","","","","","","","","","","","","","","","","","",
                "","","","","","","","","","","","","","","","","","","","","","",
                "","","","","","","","","","","","","","","","","","","","","","",
            };
            return list;
        }

        /// <summary>
        /// 从OPML文件中获取分类列表
        /// </summary>
        /// <param name="file"></param>
        /// <returns></returns>
        public async static Task<List<Category>> GetRssListFromFile(StorageFile file)
        {
            string content = await FileIO.ReadTextAsync(file);
            var opml = new Opml(content);
            var list = new List<Category>();
            var defaultCategory = new Category("Default", "");
            if (opml.Body.Outlines.Count > 0)
            {
                foreach (var outline in opml.Body.Outlines)
                {
                    if(outline.Outlines!=null && outline.Outlines.Count > 0 && string.IsNullOrEmpty(outline.XMLUrl))
                    {
                        list.Add(new Category(outline));
                    }
                    else
                    {
                        var c = new Channel(outline);
                        if(c!=null && !string.IsNullOrEmpty(c.Name))
                        {
                            defaultCategory.Channels.Add(c);
                        }
                    }
                }
            }
            if (defaultCategory.Channels.Count > 0)
            {
                list.Add(defaultCategory);
            }
            return list;
        }

        /// <summary>
        /// 异步POST指定类型的数据
        /// </summary>
        /// <typeparam name="T">实体类</typeparam>
        /// <param name="url">地址</param>
        /// <param name="data">数据</param>
        /// <param name="ts">过期时间</param>
        /// <returns>处理结果</returns>
        public async static Task<string> PostAsyncData(string url, string paramData, Dictionary<string, string> headerDic = null)
        {
            string result = string.Empty;
            try
            {
                HttpWebRequest wbRequest = (HttpWebRequest)WebRequest.Create(url);
                wbRequest.Method = "POST";
                wbRequest.ContentType = "application/x-www-form-urlencoded";
                wbRequest.Accept = "application/json";
                wbRequest.ContentLength = Encoding.UTF8.GetByteCount(paramData);
                if (headerDic != null && headerDic.Count > 0)
                {
                    foreach (var item in headerDic)
                    {
                        wbRequest.Headers.Add(item.Key, item.Value);
                    }
                }
                using (Stream requestStream = wbRequest.GetRequestStream())
                {
                    using (StreamWriter swrite = new StreamWriter(requestStream))
                    {
                        swrite.Write(paramData);
                    }
                }
                HttpWebResponse wbResponse = (await wbRequest.GetResponseAsync()) as HttpWebResponse;
                using (Stream responseStream = wbResponse.GetResponseStream())
                {
                    using (StreamReader sread = new StreamReader(responseStream))
                    {
                        result = sread.ReadToEnd();
                    }
                }
            }
            catch (Exception)
            { }

            return result;
        }

    }
}
