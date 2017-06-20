namespace ServiceControl.TransportAdapter.AcceptanceTests.Infrastructure
{
    using System;
    using System.Collections.Generic;
    using System.Reflection;
    using NServiceBus;

    public static class TypesToScanExtensions
    {
        public static void TypesToScanHack(this EndpointConfiguration config, IEnumerable<Type> types)
        {
            var method = typeof(EndpointConfiguration).GetMethod("TypesToScanInternal", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.NonPublic);
            method.Invoke(config, new object[] {types});
        }
    }
}