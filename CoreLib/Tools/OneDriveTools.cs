﻿using Microsoft.Graph;
using Microsoft.Toolkit.Services.OneDrive;
using Microsoft.Toolkit.Services.Services.MicrosoftGraph;
using Newtonsoft.Json;
using CoreLib.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Windows.Storage;
using Windows.Storage.Streams;
using CoreLib.Models.App;
using Rss.Parsers.Rss;

namespace CoreLib.Tools
{
    public class OneDriveTools
    {
        /// <summary>
        /// 应用根目录
        /// </summary>
        private OneDriveStorageFolder _appFolder = null;
        private User _user = null;
        /// <summary>
        /// 客户端ID
        /// </summary>
        private string _clientId = "your_client_id";
        /// <summary>
        /// 授权范围
        /// </summary>
        private string[] _scopes = new string[] { MicrosoftGraphScope.FilesReadWriteAppFolder, MicrosoftGraphScope.UserRead };
        /// <summary>
        /// 启动OneDrive登录授权
        /// </summary>
        /// <returns></returns>
        public async Task<bool> OneDriveAuthorize()
        {
            if (_appFolder != null)
            {
                return true;
            }
            try
            {
                // 初始化OneDrive服务实例
                bool isInit = OneDriveService.Instance.Initialize(_clientId, _scopes);
                // 确认用户完成了微软账户登录流程
                bool isLogin = await OneDriveService.Instance.LoginAsync();
                if (isInit && isLogin)
                {
                    // 获取应用文件夹
                    _appFolder = await OneDriveService.Instance.AppRootFolderAsync();
                    _user = await OneDriveService.Instance.Provider.User.GetProfileAsync();
                    string user = AppTools.GetLocalSetting(Enums.AppSettings.UserName, "");
                    if (string.IsNullOrEmpty(user))
                    {
                        AppTools.WriteLocalSetting(Enums.AppSettings.UserName, _user.DisplayName);
                    }
                    return true;
                }
                return false;
            }
            catch (Exception)
            {
                return false;
            }

        }
        public async Task Logout()
        {
            if(_appFolder!=null)
                await OneDriveService.Instance.LogoutAsync();
            _appFolder = null;
            _user = null;
        }
        /// <summary>
        /// 获取OneDrive中存储的Rss列表数据
        /// </summary>
        /// <returns></returns>
        public async Task<List<Category>> GetCategoryList()
        {
            if (_appFolder == null)
            {
                throw new UnauthorizedAccessException("You need to complete OneDrive authorization before you can get this file");
            }
            try
            {
                var file = await _appFolder.GetFileAsync("RssList.json");
                using (var stream = (await file.StorageFilePlatformService.OpenAsync()) as IRandomAccessStream)
                {
                    Stream st = WindowsRuntimeStreamExtensions.AsStreamForRead(stream);
                    st.Position = 0;
                    StreamReader sr = new StreamReader(st, Encoding.UTF8);
                    string result = sr.ReadToEnd();
                    result = result.Replace("\0", "");
                    if (string.IsNullOrEmpty(result))
                    {
                        result = "[]";
                    }
                    var list = JsonConvert.DeserializeObject<List<Category>>(result);
                    return list;
                }
            }
            catch(Exception ex)
            {
                if (ex.Message.ToLower().Contains("itemnotfound"))
                {
                    await _appFolder.StorageFolderPlatformService.CreateFileAsync("RssList.json", CreationCollisionOption.ReplaceExisting);
                    return new List<Category>();
                }
                throw;
            }
        }
        /// <summary>
        /// 获取OneDrive中存储的Page列表数据
        /// </summary>
        /// <returns></returns>
        public async Task<List<CustomPage>> GetPageList()
        {
            if (_appFolder == null)
            {
                throw new UnauthorizedAccessException("You need to complete OneDrive authorization before you can get this file");
            }
            try
            {
                var file = await _appFolder.GetFileAsync("PageList.json");
                using (var stream = (await file.StorageFilePlatformService.OpenAsync()) as IRandomAccessStream)
                {
                    Stream st = WindowsRuntimeStreamExtensions.AsStreamForRead(stream);
                    st.Position = 0;
                    StreamReader sr = new StreamReader(st, Encoding.UTF8);
                    string result = sr.ReadToEnd();
                    result = result.Replace("\0", "");
                    if (string.IsNullOrEmpty(result))
                    {
                        result = "[]";
                    }
                    var list = JsonConvert.DeserializeObject<List<CustomPage>>(result);
                    return list;
                }
            }
            catch (Exception ex)
            {
                if (ex.Message.ToLower().Contains("itemnotfound"))
                {
                    await _appFolder.StorageFolderPlatformService.CreateFileAsync("PageList.json", CreationCollisionOption.ReplaceExisting);
                    return new List<CustomPage>();
                }
                throw;
            }
        }
        /// <summary>
        /// 获取OneDrive中存储的稍后阅读列表数据
        /// </summary>
        /// <returns></returns>
        public async Task<List<RssSchema>> GetTodoList()
        {
            if (_appFolder == null)
            {
                throw new UnauthorizedAccessException("You need to complete OneDrive authorization before you can get this file");
            }
            try
            {
                var file = await _appFolder.GetFileAsync("TodoList.json");
                using (var stream = (await file.StorageFilePlatformService.OpenAsync()) as IRandomAccessStream)
                {
                    Stream st = WindowsRuntimeStreamExtensions.AsStreamForRead(stream);
                    st.Position = 0;
                    StreamReader sr = new StreamReader(st, Encoding.UTF8);
                    string result = sr.ReadToEnd();
                    result = result.Replace("\0", "");
                    if (string.IsNullOrEmpty(result))
                    {
                        result = "[]";
                    }
                    var list = JsonConvert.DeserializeObject<List<RssSchema>>(result);
                    return list;
                }
            }
            catch (Exception ex)
            {
                if (ex.Message.ToLower().Contains("itemnotfound"))
                {
                    await _appFolder.StorageFolderPlatformService.CreateFileAsync("TodoList.json", CreationCollisionOption.ReplaceExisting);
                    return new List<RssSchema>();
                }
                throw;
            }
        }
        /// <summary>
        /// 获取OneDrive中存储的通知列表数据
        /// </summary>
        /// <returns></returns>
        public async Task<List<Channel>> GetToastList()
        {
            if (_appFolder == null)
            {
                throw new UnauthorizedAccessException("You need to complete OneDrive authorization before you can get this file");
            }
            try
            {
                var file = await _appFolder.GetFileAsync("ToastList.json");
                using (var stream = (await file.StorageFilePlatformService.OpenAsync()) as IRandomAccessStream)
                {
                    Stream st = WindowsRuntimeStreamExtensions.AsStreamForRead(stream);
                    st.Position = 0;
                    StreamReader sr = new StreamReader(st, Encoding.UTF8);
                    string result = sr.ReadToEnd();
                    result = result.Replace("\0", "");
                    if (string.IsNullOrEmpty(result))
                    {
                        result = "[]";
                    }
                    var list = JsonConvert.DeserializeObject<List<Channel>>(result);
                    return list;
                }
            }
            catch (Exception ex)
            {
                if (ex.Message.ToLower().Contains("itemnotfound"))
                {
                    await _appFolder.StorageFolderPlatformService.CreateFileAsync("ToastList.json", CreationCollisionOption.ReplaceExisting);
                    return new List<Channel>();
                }
                throw;
            }
        }
        /// <summary>
        /// 获取OneDrive中存储的全文列表数据
        /// </summary>
        /// <returns></returns>
        public async Task<List<Channel>> GetReadableList()
        {
            if (_appFolder == null)
            {
                throw new UnauthorizedAccessException("You need to complete OneDrive authorization before you can get this file");
            }
            try
            {
                var file = await _appFolder.GetFileAsync("ReadableList.json");
                using (var stream = (await file.StorageFilePlatformService.OpenAsync()) as IRandomAccessStream)
                {
                    Stream st = WindowsRuntimeStreamExtensions.AsStreamForRead(stream);
                    st.Position = 0;
                    StreamReader sr = new StreamReader(st, Encoding.UTF8);
                    string result = sr.ReadToEnd();
                    result = result.Replace("\0", "");
                    if (string.IsNullOrEmpty(result))
                    {
                        result = "[]";
                    }
                    var list = JsonConvert.DeserializeObject<List<Channel>>(result);
                    return list;
                }
            }
            catch (Exception ex)
            {
                if (ex.Message.ToLower().Contains("itemnotfound"))
                {
                    await _appFolder.StorageFolderPlatformService.CreateFileAsync("ReadableList.json", CreationCollisionOption.ReplaceExisting);
                    return new List<Channel>();
                }
                throw;
            }
        }
        /// <summary>
        /// 获取OneDrive中存储的收藏列表数据
        /// </summary>
        /// <returns></returns>
        public async Task<List<RssSchema>> GetStarList()
        {
            if (_appFolder == null)
            {
                throw new UnauthorizedAccessException("You need to complete OneDrive authorization before you can get this file");
            }
            try
            {
                var file = await _appFolder.GetFileAsync("StarList.json");
                using (var stream = (await file.StorageFilePlatformService.OpenAsync()) as IRandomAccessStream)
                {
                    Stream st = WindowsRuntimeStreamExtensions.AsStreamForRead(stream);
                    st.Position = 0;
                    StreamReader sr = new StreamReader(st, Encoding.UTF8);
                    string result = sr.ReadToEnd();
                    result = result.Replace("\0", "");
                    if (string.IsNullOrEmpty(result))
                    {
                        result = "[]";
                    }
                    var list = JsonConvert.DeserializeObject<List<RssSchema>>(result);
                    return list;
                }
            }
            catch (Exception ex)
            {
                if (ex.Message.ToLower().Contains("itemnotfound"))
                {
                    await _appFolder.StorageFolderPlatformService.CreateFileAsync("StarList.json", CreationCollisionOption.ReplaceExisting);
                    return new List<RssSchema>();
                }
                throw;
            }
        }
        /// <summary>
        /// 获取OneDrive中存储的已读列表数据
        /// </summary>
        /// <returns></returns>
        public async Task<List<string>> GetReadList()
        {
            if (_appFolder == null)
            {
                throw new UnauthorizedAccessException("You need to complete OneDrive authorization before you can get this file");
            }
            try
            {
                var file = await _appFolder.GetFileAsync("ReadList.json");
                using (var stream = (await file.StorageFilePlatformService.OpenAsync()) as IRandomAccessStream)
                {
                    Stream st = WindowsRuntimeStreamExtensions.AsStreamForRead(stream);
                    st.Position = 0;
                    StreamReader sr = new StreamReader(st, Encoding.UTF8);
                    string result = sr.ReadToEnd();
                    result = result.Replace("\0", "");
                    if (string.IsNullOrEmpty(result))
                    {
                        result = "[]";
                    }
                    var list = JsonConvert.DeserializeObject<List<string>>(result);
                    return list;
                }
            }
            catch (Exception ex)
            {
                if (ex.Message.ToLower().Contains("itemnotfound"))
                {
                    await _appFolder.StorageFolderPlatformService.CreateFileAsync("ReadList.json", CreationCollisionOption.ReplaceExisting);
                    return new List<string>();
                }
                throw;
            }
        }
        /// <summary>
        /// 更新OneDrive中存储的Rss列表
        /// </summary>
        /// <param name="localFile">本地文件</param>
        /// <returns></returns>
        public async Task UpdateCategoryList(StorageFile localFile)
        {
            if (_appFolder == null)
            {
                throw new UnauthorizedAccessException("You need to complete OneDrive authorization before you can get this file");
            }
            try
            {
                using (var stream = await localFile.OpenReadAsync())
                {
                    await _appFolder.StorageFolderPlatformService.CreateFileAsync("RssList.json", CreationCollisionOption.ReplaceExisting,stream);
                    double time = AppTools.DateToTimeStamp(DateTime.Now.ToLocalTime());
                    AppTools.WriteRoamingSetting(Enums.AppSettings.BasicUpdateTime, time.ToString());
                    AppTools.WriteLocalSetting(Enums.AppSettings.BasicUpdateTime, time.ToString());
                }
            }
            catch (Exception)
            {
                return;
            }
        }
        /// <summary>
        /// 更新OneDrive中存储的自定义页面列表
        /// </summary>
        /// <param name="localFile">本地文件</param>
        /// <returns></returns>
        public async Task UpdatePageList(StorageFile localFile)
        {
            if (_appFolder == null)
            {
                throw new UnauthorizedAccessException("You need to complete OneDrive authorization before you can get this file");
            }
            try
            {
                using (var stream = await localFile.OpenReadAsync())
                {
                    await _appFolder.StorageFolderPlatformService.CreateFileAsync("PageList.json", CreationCollisionOption.ReplaceExisting, stream);
                    double time = AppTools.DateToTimeStamp(DateTime.Now.ToLocalTime());
                    AppTools.WriteRoamingSetting(Enums.AppSettings.PageUpdateTime, time.ToString());
                    AppTools.WriteLocalSetting(Enums.AppSettings.PageUpdateTime, time.ToString());
                }
            }
            catch (Exception)
            {
                return;
            }
        }
        /// <summary>
        /// 更新OneDrive中存储的待读列表
        /// </summary>
        /// <param name="localFile">列表</param>
        /// <returns></returns>
        public async Task UpdateTodoList(StorageFile localFile)
        {
            if (_appFolder == null)
            {
                throw new UnauthorizedAccessException("You need to complete OneDrive authorization before you can get this file");
            }
            try
            {
                using (var stream = await localFile.OpenReadAsync())
                {
                    await _appFolder.StorageFolderPlatformService.CreateFileAsync("TodoList.json", CreationCollisionOption.ReplaceExisting, stream);
                    double time = AppTools.DateToTimeStamp(DateTime.Now.ToLocalTime());
                    AppTools.WriteRoamingSetting(Enums.AppSettings.TodoUpdateTime, time.ToString());
                    AppTools.WriteLocalSetting(Enums.AppSettings.TodoUpdateTime, time.ToString());
                }
            }
            catch (Exception)
            {
                return;
            }
        }
        /// <summary>
        /// 更新OneDrive中存储的收藏列表
        /// </summary>
        /// <param name="localFile">列表</param>
        /// <returns></returns>
        public async Task UpdateStarList(StorageFile localFile)
        {
            if (_appFolder == null)
            {
                throw new UnauthorizedAccessException("You need to complete OneDrive authorization before you can get this file");
            }
            try
            {
                using (var stream = await localFile.OpenReadAsync())
                {
                    await _appFolder.StorageFolderPlatformService.CreateFileAsync("StarList.json", CreationCollisionOption.ReplaceExisting, stream);
                    double time = AppTools.DateToTimeStamp(DateTime.Now.ToLocalTime());
                    AppTools.WriteRoamingSetting(Enums.AppSettings.StarUpdateTime, time.ToString());
                    AppTools.WriteLocalSetting(Enums.AppSettings.StarUpdateTime, time.ToString());
                }
            }
            catch (Exception)
            {
                return;
            }
        }
        /// <summary>
        /// 更新OneDrive中存储的通知列表
        /// </summary>
        /// <param name="localFile">列表</param>
        /// <returns></returns>
        public async Task UpdateToastList(StorageFile localFile)
        {
            if (_appFolder == null)
            {
                throw new UnauthorizedAccessException("You need to complete OneDrive authorization before you can get this file");
            }
            try
            {
                using (var stream = await localFile.OpenReadAsync())
                {
                    await _appFolder.StorageFolderPlatformService.CreateFileAsync("ToastList.json", CreationCollisionOption.ReplaceExisting, stream);
                    double time = AppTools.DateToTimeStamp(DateTime.Now.ToLocalTime());
                    AppTools.WriteRoamingSetting(Enums.AppSettings.ToastUpdateTime, time.ToString());
                    AppTools.WriteLocalSetting(Enums.AppSettings.ToastUpdateTime, time.ToString());
                }
            }
            catch (Exception)
            {
                return;
            }
        }
        /// <summary>
        /// 更新OneDrive中存储的全文频道列表
        /// </summary>
        /// <param name="localFile">列表</param>
        /// <returns></returns>
        public async Task UpdateReadableList(StorageFile localFile)
        {
            if (_appFolder == null)
            {
                throw new UnauthorizedAccessException("You need to complete OneDrive authorization before you can get this file");
            }
            try
            {
                using (var stream = await localFile.OpenReadAsync())
                {
                    await _appFolder.StorageFolderPlatformService.CreateFileAsync("ReadableList.json", CreationCollisionOption.ReplaceExisting, stream);
                    double time = AppTools.DateToTimeStamp(DateTime.Now.ToLocalTime());
                    AppTools.WriteRoamingSetting(Enums.AppSettings.ReadableUpdateTime, time.ToString());
                    AppTools.WriteLocalSetting(Enums.AppSettings.ReadableUpdateTime, time.ToString());
                }
            }
            catch (Exception)
            {
                return;
            }
        }
        /// <summary>
        /// 更新OneDrive中存储的已读列表
        /// </summary>
        /// <param name="localFile">列表</param>
        /// <returns></returns>
        public async Task UpdateReadList(StorageFile localFile)
        {
            if (_appFolder == null)
            {
                throw new UnauthorizedAccessException("You need to complete OneDrive authorization before you can get this file");
            }
            try
            {
                using (var stream = await localFile.OpenReadAsync())
                {
                    await _appFolder.StorageFolderPlatformService.CreateFileAsync("ReadList.json", CreationCollisionOption.ReplaceExisting, stream);
                    double time = AppTools.DateToTimeStamp(DateTime.Now.ToLocalTime());
                    AppTools.WriteRoamingSetting(Enums.AppSettings.ReadUpdateTime, time.ToString());
                    AppTools.WriteLocalSetting(Enums.AppSettings.ReadUpdateTime, time.ToString());
                }
            }
            catch (Exception)
            {
                return;
            }
        }
    }
}
