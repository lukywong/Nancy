namespace Nancy.Bootstrapper
{
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// 
    /// </summary>
    public interface ITypeCatalog
    {
        IEnumerable<Type> TypesOf(Type type);

        IEnumerable<Type> TypesOf(Type type, ScanMode mode);

        IEnumerable<Type> TypesOf<TType>();

        IEnumerable<Type> TypesOf<TType>(ScanMode mode);
    }
}