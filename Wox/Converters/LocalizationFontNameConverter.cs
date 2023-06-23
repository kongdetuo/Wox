using Avalonia.Data.Converters;
using Avalonia.Media;
using System;
using System.Globalization;
using System.Linq;
using System.Windows.Markup;

namespace Wox.Converters
{
    public class LocalizationFontNameConverter : IValueConverter
    {
        public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if(value is FontFamily family)
            {
                var names = family.FamilyNames;
                return family.Name;
                //if(names.FirstOrDefault(p= XmlLanguage.GetLanguage(CultureInfo.CurrentCulture.Name), out var name))
                //{
                //    return name;
                //}
                //return names.First().Value;
            }
            return string.Empty;
        }

        public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}