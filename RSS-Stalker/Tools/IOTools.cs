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
            if(isOneDrive)
                await App.OneDrive.UpdateCategoryList(file);
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
                await App.OneDrive.UpdateCategoryList(file);
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
                await App.OneDrive.UpdateCategoryList(file);
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
        public async static Task ReplaceTodo(List<Feed> feeds, bool isUpdate = false)
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
        public async static Task ReplaceStar(List<Feed> feeds, bool isUpdate = false)
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
                    await App.OneDrive.UpdateStarList(file);
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
        public async static Task<List<Feed>> GetLocalTodoReadList()
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
                var list = JsonConvert.DeserializeObject<List<Feed>>(text);
                return list;
            }
            catch (Exception)
            {
                return new List<Feed>();
            }
            
        }
        /// <summary>
        /// 添加新待读文章
        /// </summary>
        /// <param name="feed"></param>
        /// <returns></returns>
        public async static Task AddTodoRead(Feed feed)
        {
            var localFolder = ApplicationData.Current.LocalFolder;
            var file = await localFolder.CreateFileAsync("TodoRead.json", CreationCollisionOption.OpenIfExists);
            string text = await FileIO.ReadTextAsync(file);
            if (string.IsNullOrEmpty(text))
            {
                text = "[]";
            }
            var list = JsonConvert.DeserializeObject<List<Feed>>(text);
            if (list.Any(p => p.FeedUrl.Equals(feed.FeedUrl, StringComparison.CurrentCultureIgnoreCase)))
            {
                throw new Exception(AppTools.GetReswLanguage("Tip_TodoRepeat"));
            }
            list.Add(feed);
            text = JsonConvert.SerializeObject(list);
            await FileIO.WriteTextAsync(file, text);
            bool isOneDrive = Convert.ToBoolean(AppTools.GetLocalSetting(CoreLib.Enums.AppSettings.IsBindingOneDrive, "False"));
            if (isOneDrive)
                await App.OneDrive.UpdateTodoList(file);

        }
        /// <summary>
        /// 删除待读文章
        /// </summary>
        /// <param name="feed"></param>
        /// <returns></returns>
        public async static Task DeleteTodoRead(Feed feed)
        {
            var localFolder = ApplicationData.Current.LocalFolder;
            var file = await localFolder.CreateFileAsync("TodoRead.json", CreationCollisionOption.OpenIfExists);
            string text = await FileIO.ReadTextAsync(file);
            if (string.IsNullOrEmpty(text))
            {
                text = "[]";
            }
            var list = JsonConvert.DeserializeObject<List<Feed>>(text);
            list.RemoveAll(p => p.Equals(feed));
            text = JsonConvert.SerializeObject(list);
            await FileIO.WriteTextAsync(file, text);
            bool isOneDrive = Convert.ToBoolean(AppTools.GetLocalSetting(CoreLib.Enums.AppSettings.IsBindingOneDrive, "False"));
            if (isOneDrive)
                await App.OneDrive.UpdateTodoList(file);
        }

        /// <summary>
        /// 获取本地保存的收藏信息
        /// </summary>
        /// <returns></returns>
        public async static Task<List<Feed>> GetLocalStarList()
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
                var list = JsonConvert.DeserializeObject<List<Feed>>(text);
                return list;
            }
            catch (Exception)
            {
                return new List<Feed>();
            }
            
        }
        /// <summary>
        /// 收藏新文章
        /// </summary>
        /// <param name="feed"></param>
        /// <returns></returns>
        public async static Task AddStar(Feed feed)
        {
            var localFolder = ApplicationData.Current.LocalFolder;
            var file = await localFolder.CreateFileAsync("Star.json", CreationCollisionOption.OpenIfExists);
            string text = await FileIO.ReadTextAsync(file);
            if (string.IsNullOrEmpty(text))
            {
                text = "[]";
            }
            var list = JsonConvert.DeserializeObject<List<Feed>>(text);
            if (list.Any(p => p.FeedUrl.Equals(feed.FeedUrl, StringComparison.CurrentCultureIgnoreCase)))
            {
                throw new Exception(AppTools.GetReswLanguage("Tip_StarRepeat"));
            }
            list.Add(feed);
            text = JsonConvert.SerializeObject(list);
            await FileIO.WriteTextAsync(file, text);
            bool isOneDrive = Convert.ToBoolean(AppTools.GetLocalSetting(CoreLib.Enums.AppSettings.IsBindingOneDrive, "False"));
            if (isOneDrive)
                await App.OneDrive.UpdateStarList(file);
        }
        /// <summary>
        /// 删除收藏文章
        /// </summary>
        /// <param name="feed"></param>
        /// <returns></returns>
        public async static Task DeleteStar(Feed feed)
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
                var list = JsonConvert.DeserializeObject<List<Feed>>(text);
                list.RemoveAll(p => p.Equals(feed));
                text = JsonConvert.SerializeObject(list);
                await FileIO.WriteTextAsync(file, text);
                bool isOneDrive = Convert.ToBoolean(AppTools.GetLocalSetting(CoreLib.Enums.AppSettings.IsBindingOneDrive, "False"));
                if (isOneDrive)
                    await App.OneDrive.UpdateStarList(file);
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
                await App.OneDrive.UpdateToastList(file);
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
            list.RemoveAll(p=>p.Id==channel.Id);
            text = JsonConvert.SerializeObject(list);
            await FileIO.WriteTextAsync(file, text);
            bool isOneDrive = Convert.ToBoolean(AppTools.GetLocalSetting(CoreLib.Enums.AppSettings.IsBindingOneDrive, "False"));
            if (isOneDrive)
                await App.OneDrive.UpdateToastList(file);
        }
        /// <summary>
        /// 添加已读文章
        /// </summary>
        /// <returns></returns>
        public static async Task AddAlreadyReadFeed(Feed feed)
        {
            var localFolder = ApplicationData.Current.LocalFolder;
            var file = await localFolder.CreateFileAsync("AlreadyReadList.json", CreationCollisionOption.OpenIfExists);
            string text = await FileIO.ReadTextAsync(file);
            if (string.IsNullOrEmpty(text))
            {
                text = "[]";
            }
            var list = JsonConvert.DeserializeObject<List<Feed>>(text);
            var time = AppTools.DateToTimeStamp(DateTime.Now.ToLocalTime());
            list.RemoveAll(p => AppTools.DateToTimeStamp(DateTime.Parse(p.Date)) < time - 604800);
            if (!list.Any(p => p.InternalID == feed.InternalID))
            {
                list.Add(feed);
                text = JsonConvert.SerializeObject(list);
                await FileIO.WriteTextAsync(file, text);
            }
        }
        /// <summary>
        /// 将本地的收藏列表、待读列表和推送列表导出
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
            tasks.Add(task1);
            tasks.Add(task2);
            tasks.Add(task3);
            await Task.WhenAll(tasks.ToArray());
            string json = JsonConvert.SerializeObject(export);
            await FileIO.WriteTextAsync(file, json);
        }
        /// <summary>
        /// 导入收藏列表、待读列表和推送列表
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
                    if (!l.Any(p=>p.InternalID==item.InternalID))
                    {
                        isChanged = true;
                        l.Add(item);
                    }
                }
                if (isChanged)
                {
                    await ReplaceTodo(l,true);
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
            tasks.Add(task1);
            tasks.Add(task2);
            tasks.Add(task3);
            await Task.WhenAll(tasks.ToArray());
        }
    }
}
