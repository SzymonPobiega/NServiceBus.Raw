using NServiceBus;
using NUnit.Framework;

[TestFixture]
public class When_pausing_and_resuming_Learning : When_pausing_and_resuming<LearningTransport>
{
    protected override void SetupTransport(TransportExtensions<LearningTransport> extensions)
    {
        extensions.ConfigureLearning();
    }
}