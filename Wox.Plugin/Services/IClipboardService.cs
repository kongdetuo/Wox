using System.Threading.Tasks;

namespace Wox.Plugin.Services
{
    /// <summary>
    /// 剪贴板服务
    /// </summary>
    public interface IClipboardService
    {

        /// <summary>
        /// 设置文本
        /// </summary>
        /// <param name="text"></param>
        /// <returns></returns>
        Task SetTextAsync(string? text);
    }
}
