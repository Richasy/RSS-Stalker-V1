using Newtonsoft.Json;
using CoreLib.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Windows.Storage;
using Windows.Storage.AccessCache;
using Windows.Storage.Pickers;
using CoreLib.Tools;
using CoreLib.Models.App;
using Microsoft.Toolkit.Uwp.Connectivity;
using Rss.Parsers.Rss;

namespace RSS_Stalker.Tools
{
    public class IOTools
    {
        /// <summary>
        /// 打开本地文件
        /// </summary>
        /// <param name="types">后缀名列表(如.jpg,.mp3等)</param>
        /// <returns>单个文件</returns>
        public async static Task<StorageFile> OpenLocalFile(params string[] types)
        {
            var picker = new FileOpenPicker();
            picker.SuggestedStartLocation = PickerLocationId.DocumentsLibrary;
            Regex typeReg = new Regex(@"^\.[a-zA-Z0-9]+$");
            foreach (var type in types)
            {
                if (type == "*" || typeReg.IsMatch(type))
                    picker.FileTypeFilter.Add(type);
                else
                    throw new InvalidCastException("文件后缀名不正确");
            }
            var file = await picker.PickSingleFileAsync();
            if (file != null)
                return file;
            else
                return null;
        }
        /// <summary>
        /// 打开本地文件夹
        /// </summary>
        /// <returns>单个文件夹</returns>
        public async static Task<StorageFolder> OpenLocalFolder()
        {
            var picker = new FolderPicker();
            picker.SuggestedStartLocation = PickerLocationId.Downloads;
            picker.FileTypeFilter.Add("*");
            var folder = await picker.PickSingleFolderAsync();
            if (folder != null)
                return folder;
            else
                return null;
        }
        /// <summary>
        /// 获取保存的文件
        /// </summary>
        /// <param name="type">文件后缀名</param>
        /// <param name="name">文件名</param>
        /// <param name="adviceFileName">建议文件名</param>
        /// <returns></returns>
        public async static Task<StorageFile> GetSaveFile(string type, string name, string adviceFileName)
        {
            var save = new FileSavePicker();
            save.DefaultFileExtension = type;
            save.SuggestedFileName = name;
            save.SuggestedStartLocation = PickerLocationId.Desktop;
            save.FileTypeChoices.Add(adviceFileName, new List<string>() { type });
            var file = await save.PickSaveFileAsync();
            return file;
        }
        /// <summary>
        /// 在本地创建一个临时文件
        /// </summary>
        /// <param name="name">文件名</param>
        /// <returns></returns>
        public async static Task<StorageFile> CreateTempFile(string name)
        {
            var localFolder = ApplicationData.Current.LocalFolder;
            var file = await localFolder.CreateFileAsync(name, CreationCollisionOption.ReplaceExisting);
            return file;
        }
        /// <summary>
        /// 将添加的指向性文件存储进维护列表以待将来使用
        /// </summary>
        /// <param name="folder">文件</param>
        /// <returns></returns>
        public static string SaveFolderPromiss(StorageFolder folder)
        {
            if (folder != null)
            {
                string token = StorageApplicationPermissions.FutureAccessList.Add(folder);
                return token;
            }
            else
            {
                return "";
            }
        }
        /// <summary>
        /// 根据ID值获取维护文件
        /// </summary>
        /// <param name="id">标识</param>
        /// <returns></returns>
        public async static Task<StorageFolder> GetPromissFolder(string token)
        {
            if (!String.IsNullOrEmpty(token))
            {
                try
                {
                    var folder = await StorageApplicationPermissions.FutureAccessList.GetFolderAsync(token);
                    return folder;
                }
                catch (Exception)
                {
                    return null;
                }
            }
            else
            {
                return null;
            }
        }

        /// <summary>
        /// 获取本地保存的标签信息
        /// </summary>
        /// <returns></returns>
        public async static Task<List<Category>> GetLocalCategories()
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
        /// 添加新标签
        /// </summary>
        /// <param name="category">标签</param>
        /// <returns></returns>
        public async static Task AddCategory(Category category)
        {
            var localFolder = ApplicationData.Current.LocalFolder;
            var file = await localFolder.CreateFileAsync("Channels.json", CreationCollisionOption.OpenIfExists);
            string text = await FileIO.ReadTextAsync(file);
            if (string.IsNullOrEmpty(text))
            {
                text = "[]";
            }
            var list = JsonConvert.DeserializeObject<List<Category>>(text);
            list.Add(category);
            text = JsonConvert.SerializeObject(list);
            await FileIO.WriteTextAsync(file, text);
            bool isOneDrive = Convert.ToBoolean(AppTools.GetLocalSetting(CoreLib.Enums.AppSettings.IsBindingOneDrive, "False"));
            if (isOneDrive)
            {
                if (NetworkHelper.Instance.ConnectionInformation.IsInternetAvailable)
                    await App.OneDrive.UpdateCategoryList(file);
                else
                    AppTools.WriteLocalSetting(CoreLib.Enums.AppSettings.IsChannelsChangeInOffline, "True");
            }

        }
        /// <summary>
        /// 更新标签
        /// </summary>
        /// <param name="category"></param>
        /// <returns></returns>
        public async static Task UpdateCategory(Category category)
        {
            var localFolder = ApplicationData.Current.LocalFolder;
            var file = await localFolder.CreateFileAsync("Channels.json", CreationCollisionOption.OpenIfExists);
            string text = await FileIO.ReadTextAsync(file);
            if (string.IsNullOrEmpty(text))
            {
                text = "[]";
            }
            var list = JsonConvert.DeserializeObject<List<Category>>(text);
            for (int i = 0; i < list.Count; i++)
            {
                if (list[i].Equals(category))
                {
                    list[i] = category;
                }
            }
            text = JsonConvert.SerializeObject(list);
            await FileIO.WriteTextAsync(file, text);
            bool isOneDrive = Convert.ToBoolean(AppTools.GetLocalSetting(CoreLib.Enums.AppSettings.IsBindingOneDrive, "False"));
            if (isOneDrive)
            {
                if (NetworkHelper.Instance.ConnectionInformation.IsInternetAvailable)
                    await App.OneDrive.UpdateCategoryList(file);
                else
                    AppTools.WriteLocalSetting(CoreLib.Enums.AppSettings.IsChannelsChangeInOffline, "True");
            }
        }
        /// <summary>
        /// 删除标签
        /// </summary>
        /// <param name="category"></param>
        /// <returns></returns>
        public async static Task DeleteCategory(Category category)
        {
            var localFolder = ApplicationData.Current.LocalFolder;
            var file = await localFolder.CreateFileAsync("Channels.json", CreationCollisionOption.OpenIfExists);
            string text = await FileIO.ReadTextAsync(file);
            if (string.IsNullOrEmpty(text))
            {
                text = "[]";
            }
            var list = JsonConvert.DeserializeObject<List<Category>>(text);
            list.RemoveAll(p => p.Equals(category));
            text = JsonConvert.SerializeObject(list);
            await FileIO.WriteTextAsync(file, text);
            bool isOneDrive = Convert.ToBoolean(AppTools.GetLocalSetting(CoreLib.Enums.AppSettings.IsBindingOneDrive, "False"));
            if (isOneDrive)
            {
                if (NetworkHelper.Instance.ConnectionInformation.IsInternetAvailable)
                    await App.OneDrive.UpdateCategoryList(file);
                else
                    AppTools.WriteLocalSetting(CoreLib.Enums.AppSettings.IsChannelsChangeInOffline, "True");
            }
        }
        /// <summary>
        /// 完全替换RSS列表
        /// </summary>
        /// <param name="categories">标签列表</param>
        /// <returns></returns>
        public async static Task ReplaceCategory(List<Category> categories, bool isUpdate = false)
        {
            try
            {
                var localFolder = ApplicationData.Current.LocalFolder;
                var file = await localFolder.CreateFileAsync("Channels.json", CreationCollisionOption.OpenIfExists);
                string text = JsonConvert.SerializeObject(categories);
                await FileIO.WriteTextAsync(file, text);
                bool isOneDrive = Convert.ToBoolean(AppTools.GetLocalSetting(CoreLib.Enums.AppSettings.IsBindingOneDrive, "False"));
                if (isUpdate && isOneDrive)
                    await App.OneDrive.UpdateCategoryList(file);
            }
            catch (Exception)
            {
                return;
            }
        }
        /// <summary>
        /// 完全替换待读列表
        /// </summary>
        /// <param name="categories">标签列表</param>
        /// <returns></returns>
        public async static Task ReplaceTodo(List<RssSchema> feeds, bool isUpdate = false)
        {
            try
            {
                var localFolder = ApplicationData.Current.LocalFolder;
                var file = await localFolder.CreateFileAsync("TodoRead.json", CreationCollisionOption.OpenIfExists);
                string text = JsonConvert.SerializeObject(feeds);
                await FileIO.WriteTextAsync(file, text);
                bool isOneDrive = Convert.ToBoolean(AppTools.GetLocalSetting(CoreLib.Enums.AppSettings.IsBindingOneDrive, "False"));
                if (isUpdate && isOneDrive)
                    await App.OneDrive.UpdateTodoList(file);
            }
            catch (Exception)
            {
                return;
            }
        }
        /// <summary>
        /// 完全替换收藏列表
        /// </summary>
        /// <param name="feeds">标签列表</param>
        /// <returns></returns>
        public async static Task ReplaceStar(List<RssSchema> feeds, bool isUpdate = false)
        {
            try
            {
                var localFolder = ApplicationData.Current.LocalFolder;
                var file = await localFolder.CreateFileAsync("Star.json", CreationCollisionOption.OpenIfExists);
                string text = JsonConvert.SerializeObject(feeds);
                await FileIO.WriteTextAsync(file, text);
                bool isOneDrive = Convert.ToBoolean(AppTools.GetLocalSetting(CoreLib.Enums.AppSettings.IsBindingOneDrive, "False"));
                if (isUpdate && isOneDrive)
                    await App.OneDrive.UpdateStarList(file);
            }
            catch (Exception)
            {
                return;
            }

        }
        /// <summary>
        /// 完全替换通知列表
        /// </summary>
        /// <param name="channels">标签列表</param>
        /// <returns></returns>
        public async static Task ReplaceToast(List<Channel> channels, bool isUpdate = false)
        {
            try
            {
                var localFolder = ApplicationData.Current.LocalFolder;
                var file = await localFolder.CreateFileAsync("ToastChannels.json", CreationCollisionOption.OpenIfExists);
                string text = JsonConvert.SerializeObject(channels);
                await FileIO.WriteTextAsync(file, text);
                bool isOneDrive = Convert.ToBoolean(AppTools.GetLocalSetting(CoreLib.Enums.AppSettings.IsBindingOneDrive, "False"));
                if (isUpdate && isOneDrive)
                    await App.OneDrive.UpdateToastList(file);
            }
            catch (Exception)
            {
                return;
            }
        }
        /// <summary>
        /// 完全替换全文列表
        /// </summary>
        /// <param name="channels">标签列表</param>
        /// <returns></returns>
        public async static Task ReplaceReadable(List<Channel> channels, bool isUpdate = false)
        {
            try
            {
                var localFolder = ApplicationData.Current.LocalFolder;
                var file = await localFolder.CreateFileAsync("ReadableChannels.json", CreationCollisionOption.OpenIfExists);
                string text = JsonConvert.SerializeObject(channels);
                await FileIO.WriteTextAsync(file, text);
                bool isOneDrive = Convert.ToBoolean(AppTools.GetLocalSetting(CoreLib.Enums.AppSettings.IsBindingOneDrive, "False"));
                if (isUpdate && isOneDrive)
                    await App.OneDrive.UpdateReadableList(file);
            }
            catch (Exception)
            {
                return;
            }
        }
        /// <summary>
        /// 获取本地保存的待阅读信息
        /// </summary>
        /// <returns></returns>
        public async static Task<List<RssSchema>> GetLocalTodoReadList()
        {
            try
            {
                var localFolder = ApplicationData.Current.LocalFolder;
                var file = await localFolder.CreateFileAsync("TodoRead.json", CreationCollisionOption.OpenIfExists);
                string text = await FileIO.ReadTextAsync(file);
                if (string.IsNullOrEmpty(text))
                {
                    text = "[]";
                }
                var list = JsonConvert.DeserializeObject<List<RssSchema>>(text);
                return list;
            }
            catch (Exception)
            {
                return new List<RssSchema>();
            }

        }
        /// <summary>
        /// 添加新待读文章
        /// </summary>
        /// <param name="feed"></param>
        /// <returns></returns>
        public async static Task AddTodoRead(RssSchema feed)
        {
            var localFolder = ApplicationData.Current.LocalFolder;
            var file = await localFolder.CreateFileAsync("TodoRead.json", CreationCollisionOption.OpenIfExists);
            string text = await FileIO.ReadTextAsync(file);
            if (string.IsNullOrEmpty(text))
            {
                text = "[]";
            }
            var list = JsonConvert.DeserializeObject<List<RssSchema>>(text);
            if (list.Any(p => p.FeedUrl.Equals(feed.FeedUrl, StringComparison.CurrentCultureIgnoreCase)))
            {
                throw new Exception(AppTools.GetReswLanguage("Tip_TodoRepeat"));
            }
            list.Add(feed);
            text = JsonConvert.SerializeObject(list);
            await FileIO.WriteTextAsync(file, text);
            bool isOneDrive = Convert.ToBoolean(AppTools.GetLocalSetting(CoreLib.Enums.AppSettings.IsBindingOneDrive, "False"));
            if (isOneDrive)
            {
                if (NetworkHelper.Instance.ConnectionInformation.IsInternetAvailable)
                    await App.OneDrive.UpdateTodoList(file);
                else
                    AppTools.WriteLocalSetting(CoreLib.Enums.AppSettings.IsTodoChangeInOffline, "True");
            }
        }
        /// <summary>
        /// 删除待读文章
        /// </summary>
        /// <param name="feed"></param>
        /// <returns></returns>
        public async static Task DeleteTodoRead(RssSchema feed)
        {
            var localFolder = ApplicationData.Current.LocalFolder;
            var file = await localFolder.CreateFileAsync("TodoRead.json", CreationCollisionOption.OpenIfExists);
            string text = await FileIO.ReadTextAsync(file);
            if (string.IsNullOrEmpty(text))
            {
                text = "[]";
            }
            var list = JsonConvert.DeserializeObject<List<RssSchema>>(text);
            list.RemoveAll(p => p.Equals(feed));
            text = JsonConvert.SerializeObject(list);
            await FileIO.WriteTextAsync(file, text);
            bool isOneDrive = Convert.ToBoolean(AppTools.GetLocalSetting(CoreLib.Enums.AppSettings.IsBindingOneDrive, "False"));
            if (isOneDrive)
            {
                if (NetworkHelper.Instance.ConnectionInformation.IsInternetAvailable)
                    await App.OneDrive.UpdateTodoList(file);
                else
                    AppTools.WriteLocalSetting(CoreLib.Enums.AppSettings.IsTodoChangeInOffline, "True");
            }
        }
        /// <summary>
        /// 将本地所有更改上传至云端
        /// </summary>
        /// <returns></returns>
        public async static Task UpdateAllListToOneDrive()
        {
            var localFolder = ApplicationData.Current.LocalFolder;
            var tasks = new List<Task>();
            bool isChannelChange = Convert.ToBoolean(AppTools.GetLocalSetting(CoreLib.Enums.AppSettings.IsChannelsChangeInOffline, "False"));
            bool isTodoChange = Convert.ToBoolean(AppTools.GetLocalSetting(CoreLib.Enums.AppSettings.IsTodoChangeInOffline, "False"));
            bool isStarChange = Convert.ToBoolean(AppTools.GetLocalSetting(CoreLib.Enums.AppSettings.IsStarChangeInOffline, "False"));
            bool isToastChange = Convert.ToBoolean(AppTools.GetLocalSetting(CoreLib.Enums.AppSettings.IsToastChangeInOffline, "False"));
            bool isPageChange = Convert.ToBoolean(AppTools.GetLocalSetting(CoreLib.Enums.AppSettings.IsPageChangeInOffline, "False"));
            bool isReadChange = Convert.ToBoolean(AppTools.GetLocalSetting(CoreLib.Enums.AppSettings.IsReadChangeInOffline, "False"));
            if (isChannelChange)
            {
                tasks.Add(Task.Run(async () =>
                {
                    var file = await localFolder.CreateFileAsync("Channels.json", CreationCollisionOption.OpenIfExists);
                    await App.OneDrive.UpdateCategoryList(file);
                    AppTools.WriteLocalSetting(CoreLib.Enums.AppSettings.IsChannelsChangeInOffline, "False");
                }));
            }
            if (isTodoChange)
            {
                tasks.Add(Task.Run(async () =>
                {
                    var file = await localFolder.CreateFileAsync("TodoRead.json", CreationCollisionOption.OpenIfExists);
                    await App.OneDrive.UpdateCategoryList(file);
                    AppTools.WriteLocalSetting(CoreLib.Enums.AppSettings.IsTodoChangeInOffline, "False");
                }));
            }
            if (isStarChange)
            {
                tasks.Add(Task.Run(async () =>
                {
                    var file = await localFolder.CreateFileAsync("Star.json", CreationCollisionOption.OpenIfExists);
                    await App.OneDrive.UpdateTodoList(file);
                    AppTools.WriteLocalSetting(CoreLib.Enums.AppSettings.IsStarChangeInOffline, "False");
                }));
            }
            if (isToastChange)
            {
                tasks.Add(Task.Run(async () =>
                {
                    var file = await localFolder.CreateFileAsync("ToastChannels.json", CreationCollisionOption.OpenIfExists);
                    await App.OneDrive.UpdateToastList(file);
                    AppTools.WriteLocalSetting(CoreLib.Enums.AppSettings.IsToastChangeInOffline, "False");
                }));
            }
            if (isPageChange)
            {
                tasks.Add(Task.Run(async () =>
                {
                    var file = await localFolder.CreateFileAsync("Pages.json", CreationCollisionOption.OpenIfExists);
                    await App.OneDrive.UpdatePageList(file);
                    AppTools.WriteLocalSetting(CoreLib.Enums.AppSettings.IsPageChangeInOffline, "False");
                }));
            }
            if (isReadChange)
            {
                tasks.Add(Task.Run(async () =>
                {
                    var file = await localFolder.CreateFileAsync("ReadIds.json", CreationCollisionOption.OpenIfExists);
                    await App.OneDrive.UpdateReadList(file);
                    AppTools.WriteLocalSetting(CoreLib.Enums.AppSettings.IsReadChangeInOffline, "False");
                }));
            }
            if (tasks.Count == 0)
            {
                return;
            }
            await Task.WhenAll(tasks.ToArray());
        }
        /// <summary>
        /// 获取本地保存的收藏信息
        /// </summary>
        /// <returns></returns>
        public async static Task<List<RssSchema>> GetLocalStarList()
        {
            try
            {
                var localFolder = ApplicationData.Current.LocalFolder;
                var file = await localFolder.CreateFileAsync("Star.json", CreationCollisionOption.OpenIfExists);
                string text = await FileIO.ReadTextAsync(file);
                if (string.IsNullOrEmpty(text))
                {
                    text = "[]";
                }
                var list = JsonConvert.DeserializeObject<List<RssSchema>>(text);
                return list;
            }
            catch (Exception)
            {
                return new List<RssSchema>();
            }

        }
        /// <summary>
        /// 收藏新文章
        /// </summary>
        /// <param name="feed"></param>
        /// <returns></returns>
        public async static Task AddStar(RssSchema feed)
        {
            var localFolder = ApplicationData.Current.LocalFolder;
            var file = await localFolder.CreateFileAsync("Star.json", CreationCollisionOption.OpenIfExists);
            string text = await FileIO.ReadTextAsync(file);
            if (string.IsNullOrEmpty(text))
            {
                text = "[]";
            }
            var list = JsonConvert.DeserializeObject<List<RssSchema>>(text);
            if (list.Any(p => p.FeedUrl.Equals(feed.FeedUrl, StringComparison.CurrentCultureIgnoreCase)))
            {
                throw new Exception(AppTools.GetReswLanguage("Tip_StarRepeat"));
            }
            list.Add(feed);
            text = JsonConvert.SerializeObject(list);
            await FileIO.WriteTextAsync(file, text);
            bool isOneDrive = Convert.ToBoolean(AppTools.GetLocalSetting(CoreLib.Enums.AppSettings.IsBindingOneDrive, "False"));
            if (isOneDrive)
            {
                if (NetworkHelper.Instance.ConnectionInformation.IsInternetAvailable)
                    await App.OneDrive.UpdateStarList(file);
                else
                    AppTools.WriteLocalSetting(CoreLib.Enums.AppSettings.IsStarChangeInOffline, "True");
            }
        }
        /// <summary>
        /// 删除收藏文章
        /// </summary>
        /// <param name="feed"></param>
        /// <returns></returns>
        public async static Task DeleteStar(RssSchema feed)
        {
            try
            {
                var localFolder = ApplicationData.Current.LocalFolder;
                var file = await localFolder.CreateFileAsync("Star.json", CreationCollisionOption.OpenIfExists);
                string text = await FileIO.ReadTextAsync(file);
                if (string.IsNullOrEmpty(text))
                {
                    text = "[]";
                }
                var list = JsonConvert.DeserializeObject<List<RssSchema>>(text);
                list.RemoveAll(p => p.Equals(feed));
                text = JsonConvert.SerializeObject(list);
                await FileIO.WriteTextAsync(file, text);
                bool isOneDrive = Convert.ToBoolean(AppTools.GetLocalSetting(CoreLib.Enums.AppSettings.IsBindingOneDrive, "False"));
                if (isOneDrive)
                {
                    if (NetworkHelper.Instance.ConnectionInformation.IsInternetAvailable)
                        await App.OneDrive.UpdateStarList(file);
                    else
                        AppTools.WriteLocalSetting(CoreLib.Enums.AppSettings.IsStarChangeInOffline, "True");
                }
            }
            catch (Exception)
            {
                return;
            }

        }
        /// <summary>
        /// 获取需要通知的频道列表
        /// </summary>
        /// <returns></returns>
        public static async Task<List<Channel>> GetNeedToastChannels()
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
        /// 添加需要通知的频道
        /// </summary>
        /// <returns></returns>
        public static async Task AddNeedToastChannel(Channel channel)
        {
            var localFolder = ApplicationData.Current.LocalFolder;
            var file = await localFolder.CreateFileAsync("ToastChannels.json", CreationCollisionOption.OpenIfExists);
            string text = await FileIO.ReadTextAsync(file);
            if (string.IsNullOrEmpty(text))
            {
                text = "[]";
            }
            var list = JsonConvert.DeserializeObject<List<Channel>>(text);
            list.Add(channel);
            text = JsonConvert.SerializeObject(list);
            await FileIO.WriteTextAsync(file, text);
            bool isOneDrive = Convert.ToBoolean(AppTools.GetLocalSetting(CoreLib.Enums.AppSettings.IsBindingOneDrive, "False"));
            if (isOneDrive)
            {
                if (NetworkHelper.Instance.ConnectionInformation.IsInternetAvailable)
                    await App.OneDrive.UpdateToastList(file);
                else
                    AppTools.WriteLocalSetting(CoreLib.Enums.AppSettings.IsToastChangeInOffline, "True");
            }
        }
        /// <summary>
        /// 移除需要通知的频道
        /// </summary>
        /// <returns></returns>
        public static async Task RemoveNeedToastChannel(Channel channel)
        {
            var localFolder = ApplicationData.Current.LocalFolder;
            var file = await localFolder.CreateFileAsync("ToastChannels.json", CreationCollisionOption.OpenIfExists);
            string text = await FileIO.ReadTextAsync(file);
            if (string.IsNullOrEmpty(text))
            {
                text = "[]";
            }
            var list = JsonConvert.DeserializeObject<List<Channel>>(text);
            list.RemoveAll(p => p.Id == channel.Id);
            text = JsonConvert.SerializeObject(list);
            await FileIO.WriteTextAsync(file, text);
            bool isOneDrive = Convert.ToBoolean(AppTools.GetLocalSetting(CoreLib.Enums.AppSettings.IsBindingOneDrive, "False"));
            if (isOneDrive)
            {
                if (NetworkHelper.Instance.ConnectionInformation.IsInternetAvailable)
                    await App.OneDrive.UpdateToastList(file);
                else
                    AppTools.WriteLocalSetting(CoreLib.Enums.AppSettings.IsToastChangeInOffline, "True");
            }
        }
        /// <summary>
        /// 获取需要全文的频道列表
        /// </summary>
        /// <returns></returns>
        public static async Task<List<Channel>> GetNeedReadableChannels()
        {
            var localFolder = ApplicationData.Current.LocalFolder;
            var file = await localFolder.CreateFileAsync("ReadableChannels.json", CreationCollisionOption.OpenIfExists);
            string text = await FileIO.ReadTextAsync(file);
            if (string.IsNullOrEmpty(text))
            {
                text = "[]";
            }
            var list = JsonConvert.DeserializeObject<List<Channel>>(text);
            return list;
        }
        /// <summary>
        /// 添加需要全文的频道
        /// </summary>
        /// <returns></returns>
        public static async Task AddNeedReadableChannel(Channel channel)
        {
            var localFolder = ApplicationData.Current.LocalFolder;
            var file = await localFolder.CreateFileAsync("ReadableChannels.json", CreationCollisionOption.OpenIfExists);
            string text = await FileIO.ReadTextAsync(file);
            if (string.IsNullOrEmpty(text))
            {
                text = "[]";
            }
            var list = JsonConvert.DeserializeObject<List<Channel>>(text);
            list.Add(channel);
            text = JsonConvert.SerializeObject(list);
            await FileIO.WriteTextAsync(file, text);
            bool isOneDrive = Convert.ToBoolean(AppTools.GetLocalSetting(CoreLib.Enums.AppSettings.IsBindingOneDrive, "False"));
            if (isOneDrive)
            {
                if (NetworkHelper.Instance.ConnectionInformation.IsInternetAvailable)
                    await App.OneDrive.UpdateReadableList(file);
                else
                    AppTools.WriteLocalSetting(CoreLib.Enums.AppSettings.IsReadableChangeInOffline, "True");
            }
        }
        /// <summary>
        /// 移除需要全文的频道
        /// </summary>
        /// <returns></returns>
        public static async Task RemoveNeedReadableChannel(Channel channel)
        {
            var localFolder = ApplicationData.Current.LocalFolder;
            var file = await localFolder.CreateFileAsync("ReadableChannels.json", CreationCollisionOption.OpenIfExists);
            string text = await FileIO.ReadTextAsync(file);
            if (string.IsNullOrEmpty(text))
            {
                text = "[]";
            }
            var list = JsonConvert.DeserializeObject<List<Channel>>(text);
            list.RemoveAll(p => p.Id == channel.Id);
            text = JsonConvert.SerializeObject(list);
            await FileIO.WriteTextAsync(file, text);
            bool isOneDrive = Convert.ToBoolean(AppTools.GetLocalSetting(CoreLib.Enums.AppSettings.IsBindingOneDrive, "False"));
            if (isOneDrive)
            {
                if (NetworkHelper.Instance.ConnectionInformation.IsInternetAvailable)
                    await App.OneDrive.UpdateReadableList(file);
                else
                    AppTools.WriteLocalSetting(CoreLib.Enums.AppSettings.IsReadableChangeInOffline, "True");
            }
        }
        /// <summary>
        /// 将本地的收藏列表、待读列表、推送列表、自定义页面和阅读记录导出
        /// </summary>
        /// <param name="file">文件</param>
        /// <returns></returns>
        public static async Task ExportLocalList(StorageFile file)
        {
            if (file == null)
                return;
            var export = new ExportModel();
            var tasks = new List<Task>();
            var task1 = Task.Run(async () =>
            {
                var l = await GetLocalTodoReadList();
                export.Todo = l;
            });
            var task2 = Task.Run(async () =>
            {
                var l = await GetLocalStarList();
                export.Star = l;
            });
            var task3 = Task.Run(async () =>
            {
                var l = await GetNeedToastChannels();
                export.Toast = l;
            });
            var task4 = Task.Run(async () =>
            {
                var l = await GetLocalPages();
                export.Pages = l;
            });
            var task5 = Task.Run(async () =>
            {
                var l = await GetReadIds();
                export.Reads = l;
            });
            var task6 = Task.Run(async () =>
            {
                var l = await GetNeedReadableChannels();
                export.Readable = l;
            });
            tasks.Add(task1);
            tasks.Add(task2);
            tasks.Add(task3);
            tasks.Add(task4);
            tasks.Add(task5);
            tasks.Add(task6);
            await Task.WhenAll(tasks.ToArray());
            string json = JsonConvert.SerializeObject(export);
            await FileIO.WriteTextAsync(file, json);
        }
        /// <summary>
        /// 导入收藏列表、待读列表、推送列表、自定义页面和阅读记录
        /// </summary>
        /// <param name="file">文件</param>
        /// <returns></returns>
        public static async Task ImportLocalList(StorageFile file)
        {
            if (file == null)
                return;
            string content = await FileIO.ReadTextAsync(file);
            var export = JsonConvert.DeserializeObject<ExportModel>(content);
            var tasks = new List<Task>();
            var task1 = Task.Run(async () =>
            {
                var l = await GetLocalTodoReadList();
                bool isChanged = false;
                foreach (var item in export.Todo)
                {
                    if (!l.Any(p => p.InternalID == item.InternalID))
                    {
                        isChanged = true;
                        l.Add(item);
                    }
                }
                if (isChanged)
                {
                    await ReplaceTodo(l, true);
                }
            });
            var task2 = Task.Run(async () =>
            {
                var l = await GetLocalStarList();
                bool isChanged = false;
                foreach (var item in export.Star)
                {
                    if (!l.Any(p => p.InternalID == item.InternalID))
                    {
                        isChanged = true;
                        l.Add(item);
                    }
                }
                if (isChanged)
                {
                    await ReplaceStar(l, true);
                }
            });
            var task3 = Task.Run(async () =>
            {
                var l = await GetNeedToastChannels();
                bool isChanged = false;
                foreach (var item in export.Toast)
                {
                    if (!l.Any(p => p.Link == item.Link))
                    {
                        isChanged = true;
                        l.Add(item);
                    }
                }
                if (isChanged)
                {
                    await ReplaceToast(l, true);
                }
            });
            var task4 = Task.Run(async () =>
            {
                var l = await GetLocalPages();
                bool isChanged = false;
                if (export.Pages != null)
                {
                    foreach (var item in export.Pages)
                    {
                        if (!l.Any(p => p.Id == item.Id))
                        {
                            isChanged = true;
                            l.Add(item);
                        }
                    }
                }

                if (isChanged)
                {
                    await ReplacePage(l, true);
                }
            });
            var task5 = Task.Run(async () =>
            {
                var l = await GetReadIds();
                bool isChanged = false;
                if (export.Pages != null)
                {
                    foreach (var item in export.Reads)
                    {
                        if (!l.Any(p => p == item))
                        {
                            isChanged = true;
                            l.Add(item);
                        }
                    }
                }

                if (isChanged)
                {
                    await ReplaceReadIds(l);
                }
            });
            var task6 = Task.Run(async () =>
            {
                var l = await GetNeedReadableChannels();
                bool isChanged = false;
                foreach (var item in export.Readable)
                {
                    if (!l.Any(p => p.Link == item.Link))
                    {
                        isChanged = true;
                        l.Add(item);
                    }
                }
                if (isChanged)
                {
                    await ReplaceToast(l, true);
                }
            });
            tasks.Add(task1);
            tasks.Add(task2);
            tasks.Add(task3);
            tasks.Add(task4);
            tasks.Add(task5);
            tasks.Add(task6);
            await Task.WhenAll(tasks.ToArray());
        }
        /// <summary>
        /// 获取已读文章ID
        /// </summary>
        /// <returns></returns>
        public static async Task<List<string>> GetReadIds()
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
        /// <summary>
        /// 新增已读文章ID
        /// </summary>
        /// <returns></returns>
        public static async Task ReplaceReadIds(List<string> articleIds)
        {
            var localFolder = ApplicationData.Current.LocalFolder;
            var file = await localFolder.CreateFileAsync("ReadIds.json", CreationCollisionOption.ReplaceExisting);
            string text = JsonConvert.SerializeObject(articleIds);
            await FileIO.WriteTextAsync(file, text);
            bool isOneDrive = Convert.ToBoolean(AppTools.GetLocalSetting(CoreLib.Enums.AppSettings.IsBindingOneDrive, "False"));
            if (isOneDrive)
            {
                if (NetworkHelper.Instance.ConnectionInformation.IsInternetAvailable)
                    await App.OneDrive.UpdateReadList(file);
                else
                    AppTools.WriteLocalSetting(CoreLib.Enums.AppSettings.IsReadChangeInOffline, "True");
            }
        }
        /// <summary>
        /// 获取缓存文件的大小
        /// </summary>
        /// <returns></returns>
        public static async Task<string> GetCacheSize()
        {
            var localFolder = ApplicationData.Current.LocalFolder;
            var file1 = await localFolder.CreateFileAsync("CacheList.json", CreationCollisionOption.OpenIfExists);
            var file2 = await localFolder.CreateFileAsync("CachePageList.json", CreationCollisionOption.OpenIfExists);
            var pr1 = await file1.GetBasicPropertiesAsync();
            var pr2 = await file2.GetBasicPropertiesAsync();
            return ((pr1.Size + pr2.Size) / 1000) + " kb";
        }
        /// <summary>
        /// 删除缓存文件
        /// </summary>
        /// <returns></returns>
        public static async Task DeleteCache()
        {
            var localFolder = ApplicationData.Current.LocalFolder;
            var file = await localFolder.CreateFileAsync("CacheList.json", CreationCollisionOption.OpenIfExists);
            var file2 = await localFolder.CreateFileAsync("CachePageList.json", CreationCollisionOption.OpenIfExists);
            await FileIO.WriteTextAsync(file, "[]");
            await FileIO.WriteTextAsync(file2, "[]");
        }
        /// <summary>
        /// 获取缓存列表
        /// </summary>
        /// <returns></returns>
        public static async Task<List<CacheModel>> GetCacheChannels()
        {
            var localFolder = ApplicationData.Current.LocalFolder;
            var file = await localFolder.CreateFileAsync("CacheList.json", CreationCollisionOption.OpenIfExists);
            string content = await FileIO.ReadTextAsync(file);
            if (string.IsNullOrEmpty(content))
            {
                content = "[]";
            }
            var list = JsonConvert.DeserializeObject<List<CacheModel>>(content);
            return list;
        }
        /// <summary>
        /// 获取缓存列表
        /// </summary>
        /// <returns></returns>
        public static async Task<List<CacheModel>> GetCachePages()
        {
            var localFolder = ApplicationData.Current.LocalFolder;
            var file = await localFolder.CreateFileAsync("CachePageList.json", CreationCollisionOption.OpenIfExists);
            string content = await FileIO.ReadTextAsync(file);
            if (string.IsNullOrEmpty(content))
            {
                content = "[]";
            }
            var list = JsonConvert.DeserializeObject<List<CacheModel>>(content);
            return list;
        }
        /// <summary>
        /// 升级缓存
        /// </summary>
        /// <param name="channels">需要缓存的频道列表</param>
        /// <returns></returns>
        public static async Task AddCacheChannel(Action<int> ProgressHandle, params Channel[] channels)
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
            int completeCount = 0;
            if (list.Length > 0)
            {
                foreach (var channel in list)
                {
                    tasks.Add(Task.Run(async () =>
                    {
                        var articles = await AppTools.GetFeedsFromUrl(channel.Link, true);
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
                        completeCount += 1;
                        if (ProgressHandle != null)
                        {
                            ProgressHandle(completeCount);
                        }
                    }));
                }
                await Task.WhenAll(tasks.ToArray());
                content = JsonConvert.SerializeObject(results);
                await FileIO.WriteTextAsync(file, content);
            }
        }
        /// <summary>
        /// 升级缓存
        /// </summary>
        /// <param name="data">需要缓存的频道列表</param>
        /// <returns></returns>
        public static async Task AddCacheChannel(List<CacheModel> data)
        {
            var localFolder = ApplicationData.Current.LocalFolder;
            var file = await localFolder.CreateFileAsync("CacheList.json", CreationCollisionOption.OpenIfExists);
            string content = await FileIO.ReadTextAsync(file);
            if (string.IsNullOrEmpty(content))
            {
                content = "[]";
            }
            var results = JsonConvert.DeserializeObject<List<CacheModel>>(content);
            if (results.Count > 0)
            {
                results.RemoveAll(p => data.Any(c => c.Channel.Id == p.Channel.Id));
            }
            results.AddRange(data);
            content = JsonConvert.SerializeObject(results);
            await FileIO.WriteTextAsync(file, content);
        }
        /// <summary>
        /// 升级缓存
        /// </summary>
        /// <param name="channels">需要缓存的页面列表</param>
        /// <returns></returns>
        public static async Task AddCachePage(Action<int> ProgressHandle, params CustomPage[] pages)
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
            int completeCount = 0;
            if (list.Length > 0)
            {
                foreach (var page in list)
                {
                    tasks.Add(Task.Run(async () =>
                    {
                        int now = AppTools.DateToTimeStamp(DateTime.Now.ToLocalTime());
                        var articles = await AppTools.GetSchemaFromPage(page);
                        if (results.Any(p => p.Page.Id == page.Id))
                        {
                            var target = results.Where(p => p.Page.Id == page.Id).First();
                            target.Feeds = articles;
                            target.CacheTime = now;
                        }
                        else
                        {
                            results.Add(new CacheModel() { Page = page, Feeds = articles, CacheTime = now });
                        }
                        completeCount += 1;
                        if (ProgressHandle != null)
                        {
                            ProgressHandle(completeCount);
                        }
                    }));
                }
                await Task.WhenAll(tasks.ToArray());
                content = JsonConvert.SerializeObject(results);
                await FileIO.WriteTextAsync(file, content);
            }
        }
        /// <summary>
        /// 获取本地的文章缓存
        /// </summary>
        /// <param name="channel">频道</param>
        /// <returns></returns>
        public static async Task<Tuple<List<RssSchema>, int>> GetLocalCache(Channel channel)
        {
            var list = await GetCacheChannels();
            var results = new List<RssSchema>();
            int time = 0;
            if (list.Count > 0)
            {
                var cache = list.Where(p => p.Channel.Link == channel.Link).FirstOrDefault();
                if (cache != null)
                {
                    foreach (var item in cache.Feeds)
                    {
                        results.Add(item);
                    }
                    time = cache.CacheTime;
                }
            }
            return new Tuple<List<RssSchema>, int>(results, time);
        }
        /// <summary>
        /// 获取本地的页面缓存
        /// </summary>
        /// <param name="page">页面</param>
        /// <returns></returns>
        public static async Task<Tuple<List<RssSchema>, int>> GetLocalCache(CustomPage page)
        {
            var list = await GetCachePages();
            var results = new List<RssSchema>();
            int time = 0;
            if (list.Count > 0)
            {
                var cache = list.Where(p => p.Page.Id == page.Id).FirstOrDefault();
                if (cache != null)
                {
                    foreach (var item in cache.Feeds)
                    {
                        results.Add(item);
                    }
                    time = cache.CacheTime;
                }

            }
            return new Tuple<List<RssSchema>, int>(results, time);
        }

        /// <summary>
        /// 获取本地保存的自定义页面信息
        /// </summary>
        /// <returns></returns>
        public async static Task<List<CustomPage>> GetLocalPages()
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
        /// 添加新页面
        /// </summary>
        /// <param name="page">页面</param>
        /// <returns></returns>
        public async static Task AddPage(CustomPage page)
        {
            var localFolder = ApplicationData.Current.LocalFolder;
            var file = await localFolder.CreateFileAsync("Pages.json", CreationCollisionOption.OpenIfExists);
            string text = await FileIO.ReadTextAsync(file);
            if (string.IsNullOrEmpty(text))
            {
                text = "[]";
            }
            var list = JsonConvert.DeserializeObject<List<CustomPage>>(text);
            list.Add(page);
            text = JsonConvert.SerializeObject(list);
            await FileIO.WriteTextAsync(file, text);
            bool isOneDrive = Convert.ToBoolean(AppTools.GetLocalSetting(CoreLib.Enums.AppSettings.IsBindingOneDrive, "False"));
            if (isOneDrive)
            {
                if (NetworkHelper.Instance.ConnectionInformation.IsInternetAvailable)
                    await App.OneDrive.UpdatePageList(file);
                else
                    AppTools.WriteLocalSetting(CoreLib.Enums.AppSettings.IsPagesChangeInOffline, "True");
            }

        }
        /// <summary>
        /// 更新页面
        /// </summary>
        /// <param name="page"></param>
        /// <returns></returns>
        public async static Task UpdatePage(CustomPage page)
        {
            var localFolder = ApplicationData.Current.LocalFolder;
            var file = await localFolder.CreateFileAsync("Pages.json", CreationCollisionOption.OpenIfExists);
            string text = await FileIO.ReadTextAsync(file);
            if (string.IsNullOrEmpty(text))
            {
                text = "[]";
            }
            var list = JsonConvert.DeserializeObject<List<CustomPage>>(text);
            for (int i = 0; i < list.Count; i++)
            {
                if (list[i].Equals(page))
                {
                    list[i] = page;
                }
            }
            text = JsonConvert.SerializeObject(list);
            await FileIO.WriteTextAsync(file, text);
            bool isOneDrive = Convert.ToBoolean(AppTools.GetLocalSetting(CoreLib.Enums.AppSettings.IsBindingOneDrive, "False"));
            if (isOneDrive)
            {
                if (NetworkHelper.Instance.ConnectionInformation.IsInternetAvailable)
                    await App.OneDrive.UpdatePageList(file);
                else
                    AppTools.WriteLocalSetting(CoreLib.Enums.AppSettings.IsPagesChangeInOffline, "True");
            }
        }
        /// <summary>
        /// 删除页面
        /// </summary>
        /// <param name="page"></param>
        /// <returns></returns>
        public async static Task DeletePage(CustomPage page)
        {
            var localFolder = ApplicationData.Current.LocalFolder;
            var file = await localFolder.CreateFileAsync("Pages.json", CreationCollisionOption.OpenIfExists);
            string text = await FileIO.ReadTextAsync(file);
            if (string.IsNullOrEmpty(text))
            {
                text = "[]";
            }
            var list = JsonConvert.DeserializeObject<List<CustomPage>>(text);
            list.RemoveAll(p => p.Equals(page));
            text = JsonConvert.SerializeObject(list);
            await FileIO.WriteTextAsync(file, text);
            bool isOneDrive = Convert.ToBoolean(AppTools.GetLocalSetting(CoreLib.Enums.AppSettings.IsBindingOneDrive, "False"));
            if (isOneDrive)
            {
                if (NetworkHelper.Instance.ConnectionInformation.IsInternetAvailable)
                    await App.OneDrive.UpdatePageList(file);
                else
                    AppTools.WriteLocalSetting(CoreLib.Enums.AppSettings.IsPagesChangeInOffline, "True");
            }
        }
        /// <summary>
        /// 完全替换Page列表
        /// </summary>
        /// <param name="pages">Page列表</param>
        /// <returns></returns>
        public async static Task ReplacePage(List<CustomPage> pages, bool isUpdate = false)
        {
            try
            {
                var localFolder = ApplicationData.Current.LocalFolder;
                var file = await localFolder.CreateFileAsync("Pages.json", CreationCollisionOption.OpenIfExists);
                string text = JsonConvert.SerializeObject(pages);
                await FileIO.WriteTextAsync(file, text);
                bool isOneDrive = Convert.ToBoolean(AppTools.GetLocalSetting(CoreLib.Enums.AppSettings.IsBindingOneDrive, "False"));
                if (isUpdate && isOneDrive)
                    await App.OneDrive.UpdatePageList(file);
            }
            catch (Exception)
            {
                return;
            }
        }
        /// <summary>
        /// 从Page获取解析后的Item的信息
        /// </summary>
        /// <param name="page">地址</param>
        /// <returns></returns>
        public static async Task<List<RssSchema>> GetSchemaFromPage(CustomPage page, bool isLimit = false)
        {
            var allList = new List<RssSchema>();
            var tasks = new List<Task>();
            int now = AppTools.DateToTimeStamp(DateTime.Now.ToLocalTime());
            var caches = await GetCacheChannels();
            foreach (var item in page.Channels)
            {
                if (!string.IsNullOrEmpty(item.Link))
                {
                    tasks.Add(Task.Run(async () =>
                    {
                        var schemas = new List<RssSchema>();
                        var cache = caches.Where(c => c.Channel.Id == item.Id).FirstOrDefault();
                        if (cache != null && (now - cache.CacheTime) <= 1200)
                        {
                            schemas = cache.Feeds;
                        }
                        else
                        {
                            schemas = await AppTools.GetFeedsFromUrl(item.Link, isLimit);
                        }

                        if (page.Rules.Count > 0)
                        {
                            foreach (var rule in page.Rules)
                            {
                                schemas = AppTools.FilterRssList(schemas, rule);
                            }
                        }
                        foreach (var s in schemas)
                        {
                            allList.Add(s);
                        }
                    }));
                }
            }
            await Task.WhenAll(tasks.ToArray());
            return allList;
        }
    }
}
