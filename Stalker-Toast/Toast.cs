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
        /// <summary>
        /// 从文件中获取需要通知的源，利用Task的并行，最大限度地获取选取源的更新信息
        /// 以获取源的第一篇文章为参照，若该文章已读，说明用户已经打开频道看过了，此时没有通知的必要
        /// 若第一篇文章已经出现在已推送列表中，表面该源没有更新文章，同样不推送
        /// 若以上条件均不满足，则推送第一篇文章进行简单提醒即可。
        /// </summary>
        /// <param name="taskInstance"></param>
        async void IBackgroundTask.Run(IBackgroundTaskInstance taskInstance)
        {
            var def=taskInstance.GetDeferral();
            var toastList = await GetNeedToastChannels();
            var historyList = await GetToastHistory();
            var tasks = new List<Task>();
            var readList = await GetAlreadyReadFeed();
            foreach (var t in toastList)
            {

                tasks.Add(Task.Run(async () =>
                {
                    try
                    {
                        var items = await AppTools.GetSchemaFromUrl(t.Link);
                        if (items != null && items.Count > 0)
                        {
                            string title = t.Name;
                            string content = string.Empty;
                            var history = historyList.Where(h => h.ChannelId == t.Id).FirstOrDefault();
                            var first = items.First();
                            if (history == null)
                            {
                                historyList.Add(new ChannelTarget() { ChannelId = t.Id, LastArticleId = first.InternalID });
                                if (!readList.Any(p => p == first.InternalID))
                                {
                                    content = first.Title;
                                }
                            }
                            else
                            {
                                if (first.InternalID != history.LastArticleId)
                                {
                                    if (!readList.Any(p => p == first.InternalID))
                                    {
                                        content = first.Title;
                                        history.LastArticleId = first.InternalID;
                                    }
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
        /// <summary>
        /// 获取已读文章
        /// </summary>
        /// <returns></returns>
        private async Task<List<string>> GetAlreadyReadFeed()
        {
            var localFolder = ApplicationData.Current.LocalFolder;
            var file = await localFolder.CreateFileAsync("ReadIds.json", CreationCollisionOption.OpenIfExists);
            string text = await FileIO.ReadTextAsync(file);
            if (string.IsNullOrEmpty(text))
            {
                text = "[]";
            }
            var list = JsonConvert.DeserializeObject<List<string>>(text);
            return list;
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
