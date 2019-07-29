using CoreLib.Tools;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Data;

namespace CoreLib.Models.Converter
{
    public class ShowUnreadConverter:IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            if(value is string yo)
            {
                bool isShow = System.Convert.ToBoolean(AppTools.GetLocalSetting(Enums.AppSettings.IsShowNoRead, "True"));
                return isShow ? Visibility.Visible : Visibility.Collapsed;
            }
            return Visibility.Visible;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }
}
