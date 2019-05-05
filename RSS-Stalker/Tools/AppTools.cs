using Microsoft.Toolkit.Parsers.Rss;
using RSS_Stalker.Enums;
using RSS_Stalker.Models;
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

namespace RSS_Stalker.Tools
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
        /// 初始化标题栏颜色
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
        public static SolidColorBrush GetThemeSolidColorBrush(string key)
        {
            return (SolidColorBrush)Windows.UI.Xaml.Application.Current.Resources[key];
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
        /// 从URL获取解析后的Channel的信息
        /// </summary>
        /// <param name="url">地址</param>
        /// <returns></returns>
        public static async Task<Channel> GetChannelFromUrl(string url)
        {
            var client = new SyndicationClient();
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
        public static async Task<List<Feed>> GetScheamFromUrl(string url)
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
                var parser = new RssParser();
                var rss = parser.Parse(feed);
                
                foreach (var item in rss)
                {
                    list.Add(new Feed(item));
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

        public static List<string> GetIcons()
        {
            var list = new List<string>
            {
                "","","","","","","","","","","","","","","","","","","","","","",
                "","","","","","","","","","","","","","","","","","","","","","",
                "","","","","","","","","","","","","","","","","","","","","","",
                "","","","","","","","","","","","","","","","","","","","","","",
                "","","","","","","","","","","","","","","","","","","","","","",
                "","","","","","","","","","","","","","","","","","","","","","",
                "","","","","","","","","","","","","","","","","","","",""
            };
            return list;
        }
    }
}
