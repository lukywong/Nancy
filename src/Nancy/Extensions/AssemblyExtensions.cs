namespace Nancy.Extensions
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Reflection;

    using Nancy.Bootstrapper;

    /// <summary>
    /// Assembly extension methods
    /// </summary>
    public static class AssemblyExtensions
    {
        /// <summary>
        /// Gets exported types from an assembly and catches common errors
        /// that occur when running under test runners.
        /// </summary>
        /// <param name="assembly">Assembly to retreive from</param>
        /// <returns>An array of types</returns>
        public static Type[] SafeGetExportedTypes(this Assembly assembly)
        {
            Type[] types;

            try
            {
                types = assembly.GetExportedTypes();
            }
            catch (FileNotFoundException)
            {
                types = new Type[] { };
            }
            catch (NotSupportedException)
            {
                types = new Type[] { };
            }

            return types;
        }

        /// <summary>
        /// Gets all types implementing a particular interface/base class
        /// </summary>
        /// <typeparam name="TType">Type to search for</typeparam>
        /// <returns>An <see cref="IEnumerable{T}"/> of types.</returns>
        /// <remarks>Will scan with <see cref="ScanMode.All"/>.</remarks>
        public static IEnumerable<Type> TypesOf<TType>(this IEnumerable<Assembly> assemblies)
        {
            return assemblies
                .SelectMany(assembly => assembly.SafeGetExportedTypes())
                .Where(type => !type.IsAbstract)
                .Where(type => typeof(TType).IsAssignableFrom(type));
        }
    }
}
