using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.CompilerServices;
using static System.Net.Mime.MediaTypeNames;

namespace Wox.Plugin
{
    public static class SettingExtensions
    {
        public static bool FirstCheckBoxValue(this IEnumerable<PluginOption> options, string key)
        {
            return options.OfType<CheckBoxOption>().FirstOrDefault(p => p.Key == key)!.Value;
        }
        public static object ComboboxValue(this IEnumerable<PluginOption> options, string key)
        {
            return (options.OfType<ComboBoxOption>().FirstOrDefault(p => p.Key == key).SelectedItem.Value);
        }

        public static object? FirstValue(this IEnumerable<PluginOption> options, string key)
        {
            return options.FirstOrDefault(p => p.Key == key)?.GetValue();
        }
    }

    public abstract class PluginOption
    {
        public required string Key { get; init; }

        public abstract object? GetValue();

        public abstract void Save();
    }

    public class ComboBoxOptionItem 
    {
        public ComboBoxOptionItem(string description, object value)
        {
            Description = description;
            Value = value;
        }


        public string Description { get; set; }

        public object Value { get; set; }

    }

    public class ComboBoxOption : PluginOption
    {
        public IList<ComboBoxOptionItem> Items { get; set; }

        public ComboBoxOptionItem? SelectedItem { get; set; }

        public object? Value
        {
            get => SelectedItem?.Value;
            set => SelectedItem = Items.FirstOrDefault(p => Object.Equals( p.Value ,value));
        }

        public override object? GetValue()
        {
            return Value;
        }

        public static IList<ComboBoxOptionItem> ItemsFromEnum<TEnum>()
            where TEnum : Enum
        {
            static string? getLocalizedDescription(FieldInfo @field)
            {
                return field.GetCustomAttribute<DescriptionAttribute>()?.Description;
            }

            FieldInfo[] fields = typeof(TEnum).GetFields(BindingFlags.Static | BindingFlags.Public);
            List<ComboBoxOptionItem> list = new ();
            foreach (var item in fields)
            {
                var localizedDescription = getLocalizedDescription(item);
                var enumValue = item.GetValue(null)!;
                var key = !String.IsNullOrEmpty(localizedDescription) ? localizedDescription! : enumValue.ToString()!;
                list.Add(new (key, enumValue));
            }
            return list;
        }

        public Action<object>? WhenValueChanged { get; init; }
        public override void Save()
        {
            WhenValueChanged?.Invoke(Value);
        }
    }

    public class NumberBoxOption : PluginOption
    {
        public decimal Value { get; set; }

        public decimal? MinValue { get; set; }

        public decimal? MaxValue { get; set; }

        public override object? GetValue()
        {
            return Value;
        }

        public Action<decimal>? WhenValueChanged { get; init; }
        public override void Save()
        {
            WhenValueChanged?.Invoke(Value);
        }
    }

    public class TextOption : PluginOption
    {
        public string Text { get; set; } = "";
        public override object? GetValue()
        {
            return Text;
        }

        public Action<string>? WhenValueChanged { get; init; }
        public override void Save()
        {
            WhenValueChanged?.Invoke(Text);
        }
    }

    public class MultilineTextOption : PluginOption
    {
        public string Text { get; set; } = "";

        public override object? GetValue()
        {
            return Text;
        }

        public Action<string>? WhenValueChanged { get; init; }
        public override void Save()
        {
            WhenValueChanged?.Invoke(Text);
        }
    }

    public class CheckBoxOption : PluginOption
    {
        public bool Value { get; set; }
        public override object? GetValue()
        {
            return Value;
        }

        public Action<bool>? WhenValueChanged { get; init; }
        public override void Save()
        {
            WhenValueChanged?.Invoke(Value);
        }
    }
}
