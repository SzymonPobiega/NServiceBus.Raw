﻿using NServiceBus;
using NServiceBus.Transport;
using NUnit.Framework;

[TestFixture]
public class When_publishing_to_another_endpoint_ASB : When_publishing_to_another_endpoint<AzureServiceBusTransport>
{
    protected override TransportDefinition SetupTransport()
    {
        return Helper.ConfigureASB();
    }
}

