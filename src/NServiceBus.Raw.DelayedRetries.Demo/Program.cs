using System;
using System.Threading.Tasks;

namespace NServiceBus.Raw.DelayedRetries.Demo
{
    using System.Collections.Generic;
    using Extensibility;
    using Routing;
    using Transport;

    class Program
    {
        static Random r = new Random();

        static void Main(string[] args)
        {
            Start().GetAwaiter().GetResult();
        }

        static async Task Start()
        {
            var delayedRetriesHandler = new DelayedRetryEndpoint<MsmqTransport>("FaultyEndpoint.Retries.Store", "FaultyEndpoint.Retries", TimeSpan.FromSeconds(3));
            var delayedRetryPolicy = new DelayedRetryErrorHandlingPolicy(2, 2, "FaultyEndpoint.Retries", "error");
            await delayedRetriesHandler.Start();

            var faultyEndpointConfig = RawEndpointConfiguration.Create("FaultyEndpoint", OnMessage, "error");
            faultyEndpointConfig.UseTransport<MsmqTransport>();
            faultyEndpointConfig.CustomErrorHandlingPolicy(delayedRetryPolicy);
            faultyEndpointConfig.AutoCreateQueue();

            var endpoint = await RawEndpoint.Start(faultyEndpointConfig);

            while (true)
            {
                Console.WriteLine("Press <enter> to send a message.");
                Console.ReadLine();

                var message = new OutgoingMessage(Guid.NewGuid().ToString(), new Dictionary<string, string>(), new byte[0]);
                var operation = new TransportOperation(message, new UnicastAddressTag("FaultyEndpoint"));
                await endpoint.SendRaw(new TransportOperations(operation), new TransportTransaction(), new ContextBag());
            }
        }

        static Task OnMessage(MessageContext message, IDispatchMessages arg2)
        {
            var attempt = 1;
            string delayedRetryHeader;
            if (message.Headers.TryGetValue("NServiceBus.Raw.DelayedRetries.Attempt", out delayedRetryHeader))
            {
                attempt = int.Parse(delayedRetryHeader);
            }
            Console.WriteLine($"Attempt {attempt}");
            var value = r.Next(5); //1 in 5 chance of succeeding.
            if (value != 0)
            {
                message.Headers["Boom?"] = "Yes!";
                Console.WriteLine("Boom!");
                throw new Exception("Boom!");
            }
            Console.WriteLine("Processed");
            return Task.FromResult(0);
        }
    }
}
