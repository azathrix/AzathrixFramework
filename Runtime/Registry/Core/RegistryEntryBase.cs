using System;

namespace Azathrix.Framework.Registry
{
    [Serializable]
    public abstract class RegistryEntryBase
    {
        public string typeName;
        public string assemblyName;
        public string displayName;
        public bool enabled = true;

        public Type GetRuntimeType()
        {
            if (string.IsNullOrEmpty(typeName) || string.IsNullOrEmpty(assemblyName))
                return null;
            return Type.GetType($"{typeName}, {assemblyName}");
        }
    }
}
