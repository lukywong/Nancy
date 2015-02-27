namespace Nancy.Bootstrapper
{
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// 
    /// </summary>
    public class DefaultTypeCatalog : ITypeCatalog
    {
        public IEnumerable<Type> TypesOf(Type type)
        {
            return AppDomainAssemblyTypeScanner.TypesOf(type);
        }

        public IEnumerable<Type> TypesOf(Type type, ScanMode mode)
        {
            return AppDomainAssemblyTypeScanner.TypesOf(type, mode);
        }

        public IEnumerable<Type> TypesOf<TType>()
        {
            return AppDomainAssemblyTypeScanner.TypesOf(typeof(TType));
        }

        public IEnumerable<Type> TypesOf<TType>(ScanMode mode)
        {
            return AppDomainAssemblyTypeScanner.TypesOf(typeof(TType), mode);
        }
    }
}