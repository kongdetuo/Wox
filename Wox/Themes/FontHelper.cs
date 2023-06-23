using Avalonia.Media;
using System;
using System.Globalization;
using System.Linq;
using System.Windows.Markup;

namespace Wox.Themes
{
    public static class FontHelper
    {
       // static FontWeightConverter fontWeightConverter = new FontWeightConverter();
        public static FontWeight GetFontWeightFromInvariantStringOrNormal(string value)
        {
            if (value == null) return FontWeight.Normal;

            try
            {
                //return (FontWeight)fontWeightConverter.ConvertFromInvariantString(value);
            }
            catch
            {
                return FontWeight.Normal;
            }
            return FontWeight.Normal;
        }

        //static FontStyleConverter fontStyleConverter = new FontStyleConverter();
        public static FontStyle GetFontStyleFromInvariantStringOrNormal(string value)
        {
            if (value == null) return FontStyle.Normal;

            try
            {
               // return (FontStyle)fontStyleConverter.ConvertFromInvariantString(value);
            }
            catch
            {
                return FontStyle.Normal;
            }
            return FontStyle.Normal;
        }

       // static FontStretchConverter fontStretchConverter = new FontStretchConverter();
        public static FontStretch GetFontStretchFromInvariantStringOrNormal(string value)
        {
            if (value == null) return FontStretch.Normal;
            try
            {
                //return (FontStretch)fontStretchConverter.ConvertFromInvariantString(value);
            }
            catch
            {
                return FontStretch.Normal;
            }
            return FontStretch.Normal;
        }

        //public static FamilyTypeface ChooseRegularFamilyTypeface(this FontFamily family)
        //{
            

        //    return family.FamilyTypefaces.OrderBy(o =>
        //    {
        //        return Math.Abs(o.Stretch.ToOpenTypeStretch() - FontStretches.Normal.ToOpenTypeStretch()) * 100 +
        //            Math.Abs(o.Weight.ToOpenTypeWeight() - FontWeights.Normal.ToOpenTypeWeight()) +
        //            (o.Style == FontStyles.Normal ? 0 : o.Style == FontStyles.Oblique ? 1 : 2) * 1000;
        //    }).FirstOrDefault() ?? family.FamilyTypefaces.FirstOrDefault();
        //}

        //public static FamilyTypeface ConvertFromInvariantStringsOrNormal(this FontFamily family, string style, string weight, string stretch)
        //{
        //    var styleObj = GetFontStyleFromInvariantStringOrNormal(style);
        //    var weightObj = GetFontWeightFromInvariantStringOrNormal(weight);
        //    var stretchObj = GetFontStretchFromInvariantStringOrNormal(stretch);
        //    return family.FamilyTypefaces.FirstOrDefault(o => o.Style == styleObj && o.Weight == weightObj && o.Stretch == stretchObj)
        //        ?? family.ChooseRegularFamilyTypeface();
        //}

        private static XmlLanguageConverter languageConverter = new XmlLanguageConverter();

        //public static bool HasCurrentCultureName(this FontFamily family)
        //{
        //    var language = XmlLanguage.GetLanguage(CultureInfo.CurrentCulture.Name);
        //    return family.FamilyNames.ContainsKey(language);
        //}

    }
}
