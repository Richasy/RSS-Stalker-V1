using Microsoft.Graph;
using Microsoft.Toolkit.Services.OneDrive;
using Microsoft.Toolkit.Services.Services.MicrosoftGraph;
using Newtonsoft.Json;
using RSS_Stalker.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Storage;
using Windows.Storage.Streams;

namespace RSS_Stalker.Tools
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
        private string _clientId = "e4ef459e-d6cb-47e7-a462-f028083d1968";
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
                var file = await _appFolder.StorageFolderPlatformService.CreateFileAsync("RssList.json",CreationCollisionOption.OpenIfExists);
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
            catch
            {
                return new List<Category>();
            }
        }
        /// <summary>
        /// 更新OneDrive中存储的Rss列表
        /// </summary>
        /// <param name="list">列表</param>
        /// <returns></returns>
        public async Task UpdateCategoryList(List<Category> list)
        {
            if (_appFolder == null)
            {
                throw new UnauthorizedAccessException("You need to complete OneDrive authorization before you can get this file");
            }
            try
            {
                var file = await _appFolder.StorageFolderPlatformService.CreateFileAsync("RssList.json",CreationCollisionOption.OpenIfExists);
                using (var stream = (await file.StorageFilePlatformService.OpenAsync()) as IRandomAccessStream)
                {
                    Stream st = WindowsRuntimeStreamExtensions.AsStreamForWrite(stream);
                    st.Position = 0;
                    StreamWriter sr = new StreamWriter(st, Encoding.UTF8);
                    string content = JsonConvert.SerializeObject(list);
                    await sr.WriteAsync(content);
                    double time = AppTools.DateToTimeStamp(DateTime.Now.ToUniversalTime());
                    AppTools.WriteRoamingSetting(Enums.AppSettings.UpdateTime, time.ToString());
                    AppTools.WriteLocalSetting(Enums.AppSettings.UpdateTime, time.ToString());
                }
            }
            catch (Exception)
            {
                return;
            }
        }
    }
}
