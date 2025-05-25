using System.Drawing;

namespace Wox.Plugin
{
    public interface IIconHelper
    {
        IconLoader FromAssociatedIcon(string filepath);

        IconLoader FromEmbededIcon(string filename, int? index);

        IconLoader FromImage(string filename);

        IconLoader FromColor(Color color);
    }


}