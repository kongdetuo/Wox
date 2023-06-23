using System.Windows.Controls;

namespace Wox.Plugin
{
    public interface ISettingProvider
    {
        Avalonia.Controls.Control CreateSettingPanel();
    }
}
