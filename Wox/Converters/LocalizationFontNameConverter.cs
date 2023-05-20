using System;
using System.Globalization;
using System.Linq;
using System.Windows.Data;
using System.Windows.Markup;
using System.Windows.Media;

namespace Wox.Converters
{
    public class LocalizationFontNameConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if(value is FontFamily family)
            {
                var names = family.FamilyNames;
                if(names.TryGetValue(XmlLanguage.GetLanguage(CultureInfo.CurrentCulture.Name), out var name))
                {
                    return name;
                }
                return names.First().Value;
            }
            return string.Empty;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}