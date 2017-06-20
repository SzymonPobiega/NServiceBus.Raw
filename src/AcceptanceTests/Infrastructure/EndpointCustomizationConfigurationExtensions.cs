namespace ServiceControl.TransportAdapter.AcceptanceTests.Infrastructure
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;
    using NServiceBus;

    static class EndpointCustomizationConfigurationExtensions
    {
        public static IEnumerable<Type> GetNestedTypes(this object scope)
        {
            var types = typeof(EndpointConfiguration).Assembly.GetTypes().ToList();
            types.AddRange(GetNestedTypeRecursive(scope.GetType()));
            return types.ToList();
        }

        static IEnumerable<Type> GetNestedTypeRecursive(Type rootType)
        {
            if (rootType == null)
            {
                throw new InvalidOperationException("Make sure you nest the endpoint infrastructure inside the TestFixture as nested classes");
            }

            yield return rootType;
            foreach (var nestedType in rootType.GetNestedTypes(BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic).SelectMany(GetNestedTypeRecursive))
            {
                yield return nestedType;
            }
        }
    }
}