﻿using NServiceBus;
using NServiceBus.Transport;
using NUnit.Framework;

[TestFixture]
public class When_sending_to_another_endpoint_Learning : When_sending_to_another_endpoint<LearningTransport>
{
    protected override TransportDefinition SetupTransport()
    {
        return Helper.ConfigureLearning();
    }
}