using Avalonia.Controls;
using System.Collections.Generic;
namespace Wox.Plugin.Sys
{
    public partial class SysSettings : UserControl
    {
        public SysSettings(List<Result> Results)
        {
            InitializeComponent();

            foreach (var Result in Results)
            {
                //lbxCommands.Items.Add(Result);
            }
        }
    }
}
