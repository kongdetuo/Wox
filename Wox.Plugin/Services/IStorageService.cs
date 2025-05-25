using System;

namespace Wox.Plugin.Services
{

    public interface IStorageService
    {
        T LoadJson<T>();
        T LoadJson<T>(String path);
    }
}
