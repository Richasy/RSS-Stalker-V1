using Newtonsoft.Json;
using RSS_Stalker.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Windows.Storage;
using Windows.Storage.AccessCache;
using Windows.Storage.Pickers;

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
            await App.OneDrive.UpdateCategoryList(file);
        }
        /// <summary>
        /// 完全替换RSS列表
        /// </summary>
        /// <param name="categories">标签列表</param>
        /// <returns></returns>
        public async static Task ReplaceCategory(List<Category> categories, bool isUpdate = false)
        {
            var localFolder = ApplicationData.Current.LocalFolder;
            var file = await localFolder.CreateFileAsync("Channels.json", CreationCollisionOption.OpenIfExists);
            string text = JsonConvert.SerializeObject(categories);
            await FileIO.WriteTextAsync(file, text);
            if (isUpdate)
                await App.OneDrive.UpdateCategoryList(file);
        }
        /// <summary>
        /// 完全替换待读列表
        /// </summary>
        /// <param name="categories">标签列表</param>
        /// <returns></returns>
        public async static Task ReplaceTodo(List<Feed> feeds, bool isUpdate = false)
        {
            var localFolder = ApplicationData.Current.LocalFolder;
            var file = await localFolder.CreateFileAsync("TodoRead.json", CreationCollisionOption.OpenIfExists);
            string text = JsonConvert.SerializeObject(feeds);
            await FileIO.WriteTextAsync(file, text);
            if (isUpdate)
                await App.OneDrive.UpdateTodoList(file);
        }
        /// <summary>
        /// 完全替换收藏列表
        /// </summary>
        /// <param name="feeds">标签列表</param>
        /// <returns></returns>
        public async static Task ReplaceStar(List<Feed> feeds, bool isUpdate = false)
        {
            var localFolder = ApplicationData.Current.LocalFolder;
            var file = await localFolder.CreateFileAsync("Star.json", CreationCollisionOption.OpenIfExists);
            string text = JsonConvert.SerializeObject(feeds);
            await FileIO.WriteTextAsync(file, text);
            if (isUpdate)
                await App.OneDrive.UpdateStarList(file);
        }
        /// <summary>
        /// 获取本地保存的待阅读信息
        /// </summary>
        /// <returns></returns>
        public async static Task<List<Feed>> GetLocalTodoReadList()
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
            await App.OneDrive.UpdateTodoList(file);
        }

        /// <summary>
        /// 获取本地保存的收藏信息
        /// </summary>
        /// <returns></returns>
        public async static Task<List<Feed>> GetLocalStarList()
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
            await App.OneDrive.UpdateStarList(file);
        }
        /// <summary>
        /// 删除收藏文章
        /// </summary>
        /// <param name="feed"></param>
        /// <returns></returns>
        public async static Task DeleteStar(Feed feed)
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
            await App.OneDrive.UpdateStarList(file);
        }
    }
}
