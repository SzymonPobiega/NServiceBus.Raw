﻿using NServiceBus;
using NServiceBus.Transport;
using NUnit.Framework;

[TestFixture]
public class When_sending_from_send_only_endpoint_Learning : When_sending_from_send_only_endpoint<LearningTransport>
{
    protected override TransportDefinition SetupTransport()
    {
        return Helper.ConfigureLearning();
    }
}