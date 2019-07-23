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
    public class ShowFeedDescriptionConverter:IValueConverter
    {
        object IValueConverter.Convert(object value, Type targetType, object parameter, string language)
        {
            if(value is string desc)
            {
                bool isShow = Convert.ToBoolean(AppTools.GetLocalSetting(Enums.AppSettings.IsShowFeedDescription, "True"));
                if (isShow)
                    return Visibility.Visible;
                else
                    return Visibility.Collapsed;
            }
            return Visibility.Visible;
        }

        object IValueConverter.ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }
}
