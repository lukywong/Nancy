namespace Nancy.Bootstrapper
{
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// Default implementation for the <see cref="ITypeCatalog"/> interface.
    /// </summary>
    /// <remarks>This implementation is a proxy to <see cref="AppDomainAssemblyTypeScanner"/>.</remarks>
    public class DefaultTypeCatalog : ITypeCatalog
    {
        /// <summary>
        /// Gets all types implementing a particular interface/base class
        /// </summary>
        /// <param name="type">Type to search for</param>
        /// <returns>An <see cref="IEnumerable{T}"/> of types.</returns>
        /// <remarks>Will scan with <see cref="ScanMode.All"/>.</remarks>
        public IEnumerable<Type> TypesOf(Type type)
        {
            return AppDomainAssemblyTypeScanner.TypesOf(type);
        }

        /// <summary>
        /// Gets all types implementing a particular interface/base class
        /// </summary>
        /// <param name="type">Type to search for</param>
        /// <param name="mode">A <see cref="ScanMode"/> value to determine which type set to scan in.</param>
        /// <returns>An <see cref="IEnumerable{T}"/> of types.</returns>
        public IEnumerable<Type> TypesOf(Type type, ScanMode mode)
        {
            return AppDomainAssemblyTypeScanner.TypesOf(type, mode);
        }

        /// <summary>
        /// Gets all types implementing a particular interface/base class
        /// </summary>
        /// <typeparam name="TType">Type to search for</typeparam>
        /// <returns>An <see cref="IEnumerable{T}"/> of types.</returns>
        /// <remarks>Will scan with <see cref="ScanMode.All"/>.</remarks>
        public IEnumerable<Type> TypesOf<TType>()
        {
            return AppDomainAssemblyTypeScanner.TypesOf(typeof(TType));
        }

        /// <summary>
        /// Gets all types implementing a particular interface/base class
        /// </summary>
        /// <typeparam name="TType">Type to search for</typeparam>
        /// <param name="mode">A <see cref="ScanMode"/> value to determine which type set to scan in.</param>
        /// <returns>An <see cref="IEnumerable{T}"/> of types.</returns>
        public IEnumerable<Type> TypesOf<TType>(ScanMode mode)
        {
            return AppDomainAssemblyTypeScanner.TypesOf(typeof(TType), mode);
        }
    }
}