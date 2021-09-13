﻿using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace Wox
{
    public partial class ResultListBox
    {
        private Point _lastpos;
        private ListBoxItem curItem = null;
        public ResultListBox()
        {
            InitializeComponent();
        }

        private void OnMouseEnter(object sender, MouseEventArgs e)
        {
            curItem = (ListBoxItem)sender;
            var p = e.GetPosition((IInputElement)sender);
            _lastpos = p;
        }

        private void OnMouseMove(object sender, MouseEventArgs e)
        {
            var p = e.GetPosition((IInputElement)sender);
            if (_lastpos != p)
            {
                ((ListBoxItem) sender).IsSelected = true;
            }
        }

        private void ListBox_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            if (curItem != null)
            {
                curItem.IsSelected = true;
            }
        }
    }
}
