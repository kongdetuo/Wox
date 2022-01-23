using System.Collections.Generic;

namespace Wox.Plugin
{
    public class PluginProxy
    {
        public IPlugin Plugin { get;  set; }
        public PluginMetadata Metadata { get;  set; }
        
        public override string ToString()
        {
            return Metadata.Name;
        }

        public override bool Equals(object obj)
        {
            PluginProxy r = obj as PluginProxy;
            if (r != null)
            {
                return string.Equals(r.Metadata.ID, Metadata.ID);
            }
            else
            {
                return false;
            }
        }

        public override int GetHashCode()
        {
            var hashcode = Metadata.ID?.GetHashCode() ?? 0;
            return hashcode;
        }
    }
}
