namespace NServiceBus.Raw
{
    using System;
    using System.Reflection;
    using Settings;
    using Transport;

    static class ConnectionString
    {
        public static string GetConnectionString(this ReadOnlySettings settings)
        {
            var transportDefinition = settings.Get<TransportDefinition>();
            var instance = settings.Get(transportSeamSettingsType.FullName);
            var prop = transportSeamSettingsType.GetProperty("TransportConnectionString");

            var transportConnectionString = prop.GetValue(instance, new object[0]);

            return (string)connectionStringGetter.Invoke(transportConnectionString, new object[] { transportDefinition });
        }

        public static void PrepareConnectionString(this SettingsHolder settings)
        {
            var ctor = transportSeamSettingsType.GetConstructors()[0];
            var transportSeamSettings = ctor.Invoke(new object[] { settings });
            settings.Set(transportSeamSettingsType.FullName, transportSeamSettings);
        }

        static Type connectionStringType = typeof(IEndpointInstance).Assembly.GetType("NServiceBus.TransportConnectionString", true);
        static Type transportSeamSettingsType = typeof(IEndpointInstance).Assembly.GetType("NServiceBus.TransportSeam+Settings", true);

        static MethodInfo connectionStringGetter = connectionStringType.GetMethod("GetConnectionStringOrRaiseError", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
    }
}