using NServiceBus;
using NUnit.Framework;

[TestFixture]
public class When_sending_from_send_only_endpoint_Learning : When_sending_from_send_only_endpoint<LearningTransport>
{
    protected override void SetupTransport(TransportExtensions<LearningTransport> extensions)
    {
        extensions.ConfigureLearning();
        //extensions.ConnectionString("sdfsd");


    }
}