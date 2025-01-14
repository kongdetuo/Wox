﻿using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using Wox.Plugin;

namespace Wox
{
    public class HighLightTextBlock
    {
        #region FontWeight


        public static FontWeight GetFontWeight(DependencyObject obj)
        {
            return (FontWeight)obj.GetValue(FontWeightProperty);
        }

        public static void SetFontWeight(DependencyObject obj, FontWeight value)
        {
            obj.SetValue(FontWeightProperty, value);
        }

        // Using a DependencyProperty as the backing store for FontWeight.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty FontWeightProperty =
            DependencyProperty.RegisterAttached("FontWeight", typeof(FontWeight), typeof(HighLightTextBlock), new PropertyMetadata(FontWeights.Normal, Refresh));


        #endregion

        #region FontStyle


        public static FontStyle GetFontStyle(DependencyObject obj)
        {
            return (FontStyle)obj.GetValue(FontStyleProperty);
        }

        public static void SetFontStyle(DependencyObject obj, FontStyle value)
        {
            obj.SetValue(FontStyleProperty, value);
        }

        // Using a DependencyProperty as the backing store for FontStyle.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty FontStyleProperty =
            DependencyProperty.RegisterAttached("FontStyle", typeof(FontStyle), typeof(HighLightTextBlock), new PropertyMetadata(FontStyles.Normal, Refresh));


        #endregion

        #region FontStretch


        public static FontStretch GetFontStretch(DependencyObject obj)
        {
            return (FontStretch)obj.GetValue(FontStretchProperty);
        }

        public static void SetFontStretch(DependencyObject obj, FontStretch value)
        {
            obj.SetValue(FontStretchProperty, value);
        }

        // Using a DependencyProperty as the backing store for FontStretch.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty FontStretchProperty =
            DependencyProperty.RegisterAttached("FontStretch", typeof(FontStretch), typeof(HighLightTextBlock), new PropertyMetadata(FontStretches.Normal, Refresh));


        #endregion

        #region Foreground


        public static Brush GetForeground(DependencyObject obj)
        {
            return (Brush)obj.GetValue(ForegroundProperty);
        }

        public static void SetForeground(DependencyObject obj, Brush value)
        {
            obj.SetValue(ForegroundProperty, value);
        }

        // Using a DependencyProperty as the backing store for Foreground.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty ForegroundProperty =
            DependencyProperty.RegisterAttached("Foreground", typeof(Brush), typeof(HighLightTextBlock), new PropertyMetadata(Brushes.Black ,Refresh));


        #endregion

        #region HighlightText

        public static HighlightText GetHighlightText(DependencyObject obj)
        {
            return (HighlightText)obj.GetValue(HighlightTextProperty);
        }

        public static void SetHighlightText(DependencyObject obj, HighlightText value)
        {
            obj.SetValue(HighlightTextProperty, value);
        }

        // Using a DependencyProperty as the backing store for HighlightText.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty HighlightTextProperty =
            DependencyProperty.RegisterAttached("HighlightText", typeof(HighlightText), typeof(HighLightTextBlock), new PropertyMetadata(null, Refresh));


        #endregion

        private static void Refresh(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var textBlock = d as TextBlock;
            var highlightText = GetHighlightText(d);
            if (textBlock is not null && highlightText is not null)
            {
                textBlock.Inlines.Clear();

                var text = highlightText.Text;
                var highlightData = highlightText.HighlightData;

                var foreground = GetForeground(d);
                var fontStretch = GetFontStretch(d);
                var fontStyle = GetFontStyle(d);
                var fontWeight = GetFontWeight(d);

                for (var i = 0; i < text.Length; i++)
                {
                    var currentCharacter = text.Substring(i, 1);
                    if (ShouldHighlight(highlightData, i))
                    {
                        textBlock.Inlines.Add(new Run(currentCharacter)
                        {
                            Foreground = foreground,
                            FontWeight = fontWeight,
                            FontStyle = fontStyle,
                            FontStretch = fontStretch
                        });
                    }
                    else
                    {
                        textBlock.Inlines.Add(new Run(currentCharacter));
                    }
                }
            }
        }
        private static bool ShouldHighlight(IList<int> highlightData, int index)
        {
            return highlightData.Contains(index);
        }
    }
}