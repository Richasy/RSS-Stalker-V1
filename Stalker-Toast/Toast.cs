using CoreLib.Models;
using CoreLib.Tools;
using Newtonsoft.Json;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.ApplicationModel.Background;
using Windows.Storage;
using Windows.UI.Notifications;

namespace StalkerToast
{
    public sealed class Toast:IBackgroundTask
    {
        async void IBackgroundTask.Run(IBackgroundTaskInstance taskInstance)
        {
            var def=taskInstance.GetDeferral();
            var toastList = await GetNeedToastChannels();
            var historyList = await GetToastHistory();
            var tasks = new List<Task>();
            foreach (var t in toastList)
            {

                tasks.Add(Task.Run(async () =>
                {
                    try
                    {
                        var items = await AppTools.GetSchemalFromUrl(t.Link);
                        if (items != null && items.Count > 0)
                        {
                            string title = t.Name;
                            string content = string.Empty;
                            var history = historyList.Where(h => h.ChannelId == t.Id).FirstOrDefault();
                            if (history == null)
                            {
                                historyList.Add(new ChannelTarget() { ChannelId = t.Id, LastArticleId = items.First().InternalID });
                                content = items.First().Title;
                            }
                            else
                            {
                                if (items.First().InternalID != history.LastArticleId)
                                {
                                    content = items.First().Title;
                                    history.LastArticleId = items.First().InternalID;
                                }
                            }
                            if (!string.IsNullOrEmpty(content))
                            {
                                SendNotification(title, content);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine(ex.InnerException.Message);
                    }
                }));
            }
            Task.WaitAll(tasks.ToArray(),20000);
            await ReplaceToastHistory(historyList);
            def.Complete();
        }
        private void SendNotification(string title,string content)
        {
            if (!string.IsNullOrEmpty(title) && !string.IsNullOrEmpty(content))
            {
                var toastXml = ToastNotificationManager.GetTemplateContent(ToastTemplateType.ToastText02);
                var elements = toastXml.GetElementsByTagName("text");
                elements[0].AppendChild(toastXml.CreateTextNode(title));
                elements[1].AppendChild(toastXml.CreateTextNode(content));
                ToastNotification notification = new ToastNotification(toastXml);
                ToastNotificationManager.CreateToastNotifier().Show(notification);
            }
        }
        private async Task<List<Channel>> GetNeedToastChannels()
        {
            var localFolder = ApplicationData.Current.LocalFolder;
            var file = await localFolder.CreateFileAsync("ToastChannels.json", CreationCollisionOption.OpenIfExists);
            string text = await FileIO.ReadTextAsync(file);
            if (string.IsNullOrEmpty(text))
            {
                text = "[]";
            }
            var list = JsonConvert.DeserializeObject<List<Channel>>(text);
            return list;
        }
        /// <summary>
        /// 获取本地保存的通知历史信息
        /// </summary>
        /// <returns></returns>
        private async Task<List<ChannelTarget>> GetToastHistory()
        {
            var localFolder = ApplicationData.Current.LocalFolder;
            var file = await localFolder.CreateFileAsync("ChannelTarget.json", CreationCollisionOption.OpenIfExists);
            string text = await FileIO.ReadTextAsync(file);
            if (string.IsNullOrEmpty(text))
            {
                text = "[]";
            }
            var list = JsonConvert.DeserializeObject<List<ChannelTarget>>(text);
            return list;
        }
        /// <summary>
        /// 替换本地保存的通知历史信息
        /// </summary>
        /// <returns></returns>
        private async Task ReplaceToastHistory(List<ChannelTarget> history)
        {
            var localFolder = ApplicationData.Current.LocalFolder;
            var file = await localFolder.CreateFileAsync("ChannelTarget.json", CreationCollisionOption.OpenIfExists);
            string text = JsonConvert.SerializeObject(history);
            await FileIO.WriteTextAsync(file, text);
        }
    }
}
