using Microsoft.UI.Xaml.Data;
using Polymerium.Abstractions.Resources;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Polymerium.App.Converters
{
    public class ResourceTypeToStringConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            if (value is ResourceType type)
            {
                return type switch
                    {
                        ResourceType.Plugin => "插件",
                        ResourceType.DataPack => "数据包",
                        ResourceType.ResourcePack => "资源包",
                        ResourceType.ShaderPack => "着色器包",
                        ResourceType.File => "文件",
                        ResourceType.Mod => "模组",
                        ResourceType.Modpack => "整合包",
                        _ => value.ToString()
                    } ?? value;
            }
            else
                return value;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }
}
