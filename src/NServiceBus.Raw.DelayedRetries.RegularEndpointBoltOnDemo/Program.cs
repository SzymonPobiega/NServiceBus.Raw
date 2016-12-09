using System;
using System.Threading.Tasks;

namespace NServiceBus.Raw.DelayedRetries.RegularEndpointBoltOnDemo
{
    class Program
    {

        static void Main(string[] args)
        {
            Start().GetAwaiter().GetResult();
        }

        static async Task Start()
        {
            var delayedRetryHandler = new DelayedRetryEndpoint<MsmqTransport>("EndpointWithoutSLR.Retries.Store", "EndpointWithoutSLR.Retries", TimeSpan.FromSeconds(3));
            await delayedRetryHandler.Start();

            var endpointWithoutDelayedRetries = new EndpointConfiguration("EndpointWithoutSLR");
            var recoverability = endpointWithoutDelayedRetries.Recoverability();
            recoverability.DisableLegacyRetriesSatellite();
            recoverability.Immediate(i => i.NumberOfRetries(2));
            recoverability.Delayed(d => d.NumberOfRetries(0));
            endpointWithoutDelayedRetries.SendFailedMessagesTo("EndpointWithoutSLR.Retries");
            endpointWithoutDelayedRetries.UsePersistence<InMemoryPersistence>();

            var endpoint = await Endpoint.Start(endpointWithoutDelayedRetries);

            while (true)
            {
                Console.WriteLine("Press <enter> to send a message");
                Console.ReadLine();

                await endpoint.SendLocal(new MyMessage()).ConfigureAwait(false);
            }
        }
    }

    class MyMessageHandler : IHandleMessages<MyMessage>
    {
        static Random r = new Random();

        public Task Handle(MyMessage message, IMessageHandlerContext context)
        {
            var attempt = 1;
            string delayedRetryHeader;
            if (context.MessageHeaders.TryGetValue("NServiceBus.Raw.DelayedRetries.Attempt", out delayedRetryHeader))
            {
                attempt = int.Parse(delayedRetryHeader);
            }
            Console.WriteLine($"Attempt {attempt}");
            var value = r.Next(5); //1 in 5 chance of succeeding.
            if (value != 0)
            {
                Console.WriteLine("Boom!");
                throw new Exception("Boom!");
            }
            Console.WriteLine("Processed");
            return Task.FromResult(0);
        }
    }

    class MyMessage : IMessage
    {
    }
}
