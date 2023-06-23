using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Documents;
using Avalonia.Media;
using System;
using System.Collections.Generic;
using System.Linq;

using Wox.Plugin;
using Wox.Themes;

namespace Wox
{
    public class HighLightTextBlock
    {
        static HighLightTextBlock()
        {
            HighlightTextProperty.Changed.Subscribe(Refresh);
        }


        #region FontWeight


        public static FontWeight GetFontWeight(Control obj)
        {
            return obj.GetValue(FontWeightProperty);
        }

        public static void SetFontWeight(Control obj, FontWeight value)
        {
            obj.SetValue(FontWeightProperty, value);
        }

        public static readonly AttachedProperty<FontWeight> FontWeightProperty =
            AvaloniaProperty.RegisterAttached<HighLightTextBlock, Control, FontWeight>("FontWeight", FontWeight.Normal);


        #endregion

        #region FontStyle


        public static FontStyle GetFontStyle(Control obj)
        {
            return obj.GetValue(FontStyleProperty);
        }

        public static void SetFontStyle(Control obj, FontStyle value)
        {
            obj.SetValue(FontStyleProperty, value);
        }

        public static readonly AttachedProperty<FontStyle> FontStyleProperty =
            AvaloniaProperty.RegisterAttached<HighLightTextBlock, Control, FontStyle>("FontStyle", FontStyle.Normal);


        #endregion

        #region FontStretch


        public static FontStretch GetFontStretch(Control obj)
        {
            return (FontStretch)obj.GetValue(FontStretchProperty);
        }

        public static void SetFontStretch(Control obj, FontStretch value)
        {
            obj.SetValue(FontStretchProperty, value);
        }

        public static readonly AttachedProperty<FontStretch> FontStretchProperty =
            AvaloniaProperty.RegisterAttached<HighLightTextBlock, Control, FontStretch>("FontStretch", FontStretch.Normal);


        #endregion

        #region Foreground


        public static IBrush GetForeground(Control obj)
        {
            return obj.GetValue(ForegroundProperty);
        }

        public static void SetForeground(Control obj, IBrush value)
        {
            obj.SetValue(ForegroundProperty, value);
        }

        public static readonly AttachedProperty<IBrush> ForegroundProperty =
            AvaloniaProperty.RegisterAttached<HighLightTextBlock, Control, IBrush>("Foreground", Brushes.AliceBlue);

        #endregion

        #region HighlightText

        public static HighlightText GetHighlightText(Control obj)
        {
            return (HighlightText)obj.GetValue(HighlightTextProperty);
        }

        public static void SetHighlightText(Control obj, HighlightText value)
        {
            obj.SetValue(HighlightTextProperty, value);
        }

        public static readonly AttachedProperty<HighlightText> HighlightTextProperty =
            AvaloniaProperty.RegisterAttached<HighLightTextBlock, Control, HighlightText>("HighlightText");


        #endregion

        private static void Refresh(AvaloniaPropertyChangedEventArgs<HighlightText> e)
        {
            var textBlock = e.Sender as TextBlock;
            var highlightText = e.NewValue.Value;
            if (textBlock == null)
                return;

            textBlock.Text = "";
            if (textBlock.Inlines == null)
                textBlock.Inlines = new InlineCollection();
            else textBlock.Inlines.Clear();
            textBlock.Inlines.Add(new Run());

            if (textBlock is not null && highlightText is not null )
            {
                var text = highlightText.Text;

                var i = 0;
                foreach (var range in highlightText.GetHighlightRanges())
                {
                    if (range.Start.Value > i)
                        textBlock.Inlines.Add(new Run(text[i..range.Start]));

                    var run = new Run(text[range])
                    {
                        [!Run.ForegroundProperty] = textBlock[!ForegroundProperty]
                    };
                    textBlock.Inlines.Add(run);

                    i = range.End.Value;
                }
                if (i < text.Length)
                    textBlock.Inlines.Add(new Run(text[i..]));
            }
        }
    }
}