using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using NServiceBus;
using NServiceBus.AcceptanceTesting;
using NServiceBus.AcceptanceTests;
using NServiceBus.Extensibility;
using NServiceBus.Transport;
using NUnit.Framework;

public abstract class When_publishing_to_another_endpoint<TTransport> : NServiceBusAcceptanceTest<TTransport>
    where TTransport : TransportDefinition, new()
{
    [Test]
    public async Task It_receives_the_message()
    {
        var secret = Guid.NewGuid();
        var headers = new Dictionary<string, string>
        {
            ["Secret"] = secret.ToString(),
            ["NServiceBus.EnclosedMessageTypes"] = "MyEvent"
        };
        var body = Encoding.UTF8.GetBytes("Hello world!");

        var result = await Scenario.Define<Context>()
            .WithRawEndpoint<TTransport, Context>(SetupTransport, "Publisher",
                onMessage: (context, scenario, dispatcher) => Task.FromResult(0),
                onStarted: (endpoint, scenario) => endpoint.Publish(typeof(MyEvent), headers, body))
            .WithRawEndpoint<TTransport, Context>(SetupTransport, "Subscriber",
                onMessage:(context, scenario, dispatcher) =>
                {
                    if (context.Headers.TryGetValue("Secret", out var receivedSecret) && receivedSecret == secret.ToString())
                    {
                        scenario.MessageReceived = true;
                        scenario.Message = Encoding.UTF8.GetString(context.Body);
                    }
                    return Task.FromResult(0);
                },
                onStarting: (endpoint, context) =>
                {
                    return endpoint.SubscriptionManager.Subscribe(typeof(MyEvent), new ContextBag());
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

public class MyEvent : IEvent
{
}