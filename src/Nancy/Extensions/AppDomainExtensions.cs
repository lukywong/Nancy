namespace Nancy.Extensions
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;

    /// <summary>
    /// <see cref="AppDomain"/> extension methods.
    /// </summary>
    public static class AppDomainExtensions
    {
        /// <summary>
        /// Gets all the <see cref="Assembly"/> instances, in the provided <see cref="AppDomain"/> that
        /// references a Nancy assembly.
        /// </summary>
        /// <param name="domain">The <see cref="AppDomain"/> to scan.</param>
        /// <returns>An <see cref="IEnumerable{T}"/> of <see cref="Assembly"/> instances.</returns>
        public static IEnumerable<Assembly> GetNancyReferencingAssemblies(this AppDomain domain)
        {
            return domain.GetAssemblies()
                .Where(assembly => assembly.GetReferencedAssemblies().Any(r => r.Name.StartsWith("Nancy", StringComparison.OrdinalIgnoreCase)))
                .Where(assembly => !assembly.GetName().Name.StartsWith("Nancy.Testing", StringComparison.OrdinalIgnoreCase))
                .Where(assembly => !assembly.IsDynamic)
                .Where(assembly => !assembly.ReflectionOnly);
        }
    }
}