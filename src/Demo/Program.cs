namespace Demo
{
    using System;
    using System.Collections.Generic;
    using System.Text;
    using System.Threading.Tasks;
    using NServiceBus;
    using NServiceBus.Extensibility;
    using NServiceBus.Raw;
    using NServiceBus.Routing;
    using NServiceBus.Transport;

    class Program
    {
        static void Main(string[] args)
        {
            Start().GetAwaiter().GetResult();
        }

        static async Task Start()
        {
            //Ensure queues are created before starting!

            var senderConfig = RawEndpointConfiguration.Create("Sender", OnReply, "error");
            senderConfig.UseTransport<MsmqTransport>();
            senderConfig.AutoCreateQueue();

            var receiverConfig = RawEndpointConfiguration.Create("Receiver", OnRequest, "error");
            receiverConfig.UseTransport<MsmqTransport>();
            receiverConfig.AutoCreateQueue();

            var sender = await RawEndpoint.Start(senderConfig).ConfigureAwait(false);
            var receiver = await RawEndpoint.Start(receiverConfig).ConfigureAwait(false);

            var encoding = Encoding.UTF8;

            var headers = new Dictionary<string, string>();
            var body = encoding.GetBytes("Ping!");
            headers["Encoding"] = encoding.WebName;
            headers["ReplyTo"] = "Sender";
            var request = new OutgoingMessage(Guid.NewGuid().ToString(), headers, body);

            var operation = new TransportOperation(
                request, 
                new UnicastAddressTag("Receiver"));

            await sender.Dispatch(
                new TransportOperations(operation), 
                new TransportTransaction(), 
                new ContextBag())
                .ConfigureAwait(false);

            Console.WriteLine("Press <enter> to exit.");
            Console.ReadLine();

            await sender.Stop().ConfigureAwait(false);
            await receiver.Stop().ConfigureAwait(false);
        }

        static Task OnReply(MessageContext context, IDispatchMessages dispatcher)
        {
            var encodingName = context.Headers["Encoding"];
            var encoding = Encoding.GetEncoding(encodingName);
            var message = encoding.GetString(context.Body);

            Console.WriteLine(message);
            return Task.FromResult(0);
        }

        static Task OnRequest(MessageContext context, IDispatchMessages dispatcher)
        {
            var replyTo = context.Headers["ReplyTo"];
            var encodingName = context.Headers["Encoding"];
            var encoding = Encoding.GetEncoding(encodingName);
            var message = encoding.GetString(context.Body);

            Console.WriteLine(message);

            var headers = new Dictionary<string, string>();
            var body = encoding.GetBytes("Pong!");
            headers["Encoding"] = encodingName;
            var response = new OutgoingMessage(Guid.NewGuid().ToString(), headers, body);

            var operation = new TransportOperation(
                response,
                new UnicastAddressTag(replyTo));

            return dispatcher.Dispatch(
                new TransportOperations(operation),
                context.TransportTransaction,
                context.Context);
        }
    }
}
