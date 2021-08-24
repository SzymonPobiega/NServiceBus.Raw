﻿using NServiceBus;
using NServiceBus.Transport;
using NServiceBus.Transport.RabbitMQ;
using NUnit.Framework;

[TestFixture]
public class When_sending_to_another_endpoint_RabbitMQ_Direct : When_sending_to_another_endpoint
{
    protected override TransportDefinition SetupTransport()
    {
        return new RabbitMQTransport(new DirectRoutingTopology(true), "host=localhost");
    }
}