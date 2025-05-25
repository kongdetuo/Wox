using Avalonia;
using Avalonia.Data.Converters;
using Avalonia.Media;
using System;
using System.Globalization;

namespace Wox.Converters
{
    public class LocalizationFontNameConverter : IValueConverter
    {
        public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is FontFamily family)
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

    public class LocalizationConverter : IValueConverter
    {

        public static LocalizationConverter Instance { get; } = new LocalizationConverter();
        public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value != null)
            {
                return App.API.GetTranslation(value.ToString());
            }
            return string.Empty;
        }

        public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}