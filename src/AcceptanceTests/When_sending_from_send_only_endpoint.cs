using NServiceBus.AcceptanceTesting;
using NServiceBus.AcceptanceTests;
using NServiceBus.Transport;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

public abstract class When_sending_from_send_only_endpoint<TTransport> : NServiceBusAcceptanceTest<TTransport>
    where TTransport : TransportDefinition, new()
{
    [Test]
    public async Task It_receives_the_message()
    {
        var secret = Guid.NewGuid();
        var headers = new Dictionary<string, string>
        {
            ["Secret"] = secret.ToString()
        };
        var body = Encoding.UTF8.GetBytes("Hello world!");

        var result = await Scenario.Define<Context>()
            .WithRawSendOnlyEndpoint<TTransport, Context>(SetupTransport(), "Sender",
                onStarted: (endpoint, scenario) => endpoint.Send("Receiver", headers, body))
            .WithRawEndpoint<TTransport, Context>(SetupTransport(), "Receiver",
                onMessage: (context, scenario, dispatcher) =>
                {
                    if (context.Headers.TryGetValue("Secret", out var receivedSecret) && receivedSecret == secret.ToString())
                    {
                        scenario.MessageReceived = true;
                        scenario.Message = Encoding.UTF8.GetString(context.Body.ToArray());
                    }
                    return Task.FromResult(0);
                })
            .Done(c => c.MessageReceived)
            .Run();

        Assert.IsTrue(result.MessageReceived);
        Assert.AreEqual("Hello world!", result.Message);
    }

    class Context : ScenarioContext
    {
        public bool MessageReceived { get; set; }
        public string Message { get; set; }
    }
}