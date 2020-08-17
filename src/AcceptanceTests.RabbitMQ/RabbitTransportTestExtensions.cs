namespace AcceptanceTests.RabbitMQ
{
    using NServiceBus;

    public static class RabbitTransportTestExtensions
    {
        public static void UseTestConnectionString(this TransportExtensions<RabbitMQTransport> extensions)
        {
            extensions.ConnectionString("host=localhost");
        }
    }
}