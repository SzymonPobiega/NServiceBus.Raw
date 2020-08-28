using System;
using System.Reflection;
using Amazon.Runtime;
using Amazon.SQS;
using NServiceBus;
using NServiceBus.Configuration.AdvancedExtensibility;
using NServiceBus.Unicast.Messages;

public static class Helper
{
    public static void ConfigureSQS(this TransportExtensions<SqsTransport> extensions)
    {
        var settings = extensions.GetSettings();
        bool isMessageType(Type t) => true;

        extensions.ClientFactory(() => new AmazonSQSClient(new EnvironmentVariablesAWSCredentials()));

        var ctor = typeof(MessageMetadataRegistry).GetConstructor(BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance, null, new[] { typeof(Func<Type, bool>) }, null);
        var instance = (MessageMetadataRegistry)ctor.Invoke(new object[] {(Func<Type, bool>)isMessageType});
        settings.Set(instance);
    }
}