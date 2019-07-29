using CoreLib.Models;
using CoreLib.Models.App;
using CoreLib.Tools;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.ApplicationModel.Background;
using Windows.Storage;

namespace AutoCache
{
    public sealed class Main : IBackgroundTask
    {
        async void IBackgroundTask.Run(IBackgroundTaskInstance taskInstance)
        {
            var def = taskInstance.GetDeferral();
            bool isOn = Convert.ToBoolean(AppTools.GetLocalSetting(CoreLib.Enums.AppSettings.IsCacheFirst, "False"));
            if (!isOn)
                return;
            var categories = await GetLocalCategories();
            var list = new List<Channel>();
            foreach (var item in categories)
            {
                foreach (var cha in item.Channels)
                {
                    list.Add(cha);
                }
            }
            await AddCacheChannel(list.ToArray());

            def.Complete();
        }
        /// <summary>
        /// 获取本地保存的标签信息
        /// </summary>
        /// <returns></returns>
        private async Task<List<Category>> GetLocalCategories()
        {
            try
            {
                var localFolder = ApplicationData.Current.LocalFolder;
                var file = await localFolder.CreateFileAsync("Channels.json", CreationCollisionOption.OpenIfExists);
                string text = await FileIO.ReadTextAsync(file);
                if (string.IsNullOrEmpty(text))
                {
                    text = "[]";
                }
                var list = JsonConvert.DeserializeObject<List<Category>>(text);
                return list;
            }
            catch (Exception)
            {
                return new List<Category>();
            }

        }
        /// <summary>
        /// 获取本地保存的自定义页面信息
        /// </summary>
        /// <returns></returns>
        private async Task<List<CustomPage>> GetLocalPages()
        {
            try
            {
                var localFolder = ApplicationData.Current.LocalFolder;
                var file = await localFolder.CreateFileAsync("Pages.json", CreationCollisionOption.OpenIfExists);
                string text = await FileIO.ReadTextAsync(file);
                if (string.IsNullOrEmpty(text))
                {
                    text = "[]";
                }
                var list = JsonConvert.DeserializeObject<List<CustomPage>>(text);
                return list;
            }
            catch (Exception)
            {
                return new List<CustomPage>();
            }

        }
        /// <summary>
        /// 升级缓存
        /// </summary>
        /// <param name="channels">需要缓存的页面列表</param>
        /// <returns></returns>
        private async Task AddCachePage(params CustomPage[] pages)
        {
            var list = pages.Distinct().ToArray();
            var localFolder = ApplicationData.Current.LocalFolder;
            var file = await localFolder.CreateFileAsync("CachePageList.json", CreationCollisionOption.OpenIfExists);
            string content = await FileIO.ReadTextAsync(file);
            if (string.IsNullOrEmpty(content))
            {
                content = "[]";
            }
            var results = JsonConvert.DeserializeObject<List<CacheModel>>(content);
            var tasks = new List<Task>();
            if (list.Length > 0)
            {
                foreach (var page in list)
                {
                    tasks.Add(Task.Run(async () =>
                    {
                        var articles = await AppTools.GetSchemaFromPage(page,true);
                        int now = AppTools.DateToTimeStamp(DateTime.Now.ToLocalTime());
                        if (results.Any(p => p.Page.Id == page.Id))
                        {
                            var target = results.Where(p => p.Page.Id == page.Id).First();
                            target.Feeds = articles;
                            target.CacheTime = now;
                        }
                        else
                        {
                            results.Add(new CacheModel() { Page = page, Feeds = articles, CacheTime=now });
                        }
                    }));
                }
                try
                {
                    await Task.WhenAll(tasks.ToArray());
                }
                catch (Exception ex)
                {
                    Debug.WriteLine(ex.Message);
                }
                finally
                {
                    content = JsonConvert.SerializeObject(results);
                    await FileIO.WriteTextAsync(file, content);
                }
                
            }
        }
        /// <summary>
        /// 升级缓存
        /// </summary>
        /// <param name="channels">需要缓存的频道列表</param>
        /// <returns></returns>
        private async Task AddCacheChannel(params Channel[] channels)
        {
            var list = channels.Distinct().ToArray();
            var localFolder = ApplicationData.Current.LocalFolder;
            var file = await localFolder.CreateFileAsync("CacheList.json", CreationCollisionOption.OpenIfExists);
            string content = await FileIO.ReadTextAsync(file);
            if (string.IsNullOrEmpty(content))
            {
                content = "[]";
            }
            var results = JsonConvert.DeserializeObject<List<CacheModel>>(content);
            var tasks = new List<Task>();
            if (list.Length > 0)
            {
                foreach (var channel in list)
                {
                    tasks.Add(Task.Run(async () =>
                    {
                        var articles = await AppTools.GetSchemaFromUrl(channel.Link,true);
                        int now = AppTools.DateToTimeStamp(DateTime.Now.ToLocalTime());
                        if (results.Any(p => p.Channel.Link == channel.Link))
                        {
                            var target = results.Where(p => p.Channel.Link == channel.Link).First();
                            target.Feeds = articles;
                            target.CacheTime = now;
                        }
                        else
                        {
                            results.Add(new CacheModel() { Channel = channel, Feeds = articles, CacheTime = now });
                        }
                    }));
                }
                try
                {
                    Task.WaitAll(tasks.ToArray(), new TimeSpan(0,0,24));
                }
                catch (Exception ex)
                {
                    Debug.WriteLine(ex.Message);
                }
                finally
                {
                    content = JsonConvert.SerializeObject(results);
                    await FileIO.WriteTextAsync(file, content);
                }
                
            }
        }
    }
}
