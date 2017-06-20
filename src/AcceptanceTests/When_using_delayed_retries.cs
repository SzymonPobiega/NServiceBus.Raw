using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using NServiceBus.AcceptanceTesting;
using NServiceBus.AcceptanceTests;
using NServiceBus.Raw.DelayedRetries;
using NUnit.Framework;

[TestFixture]
public class When_using_delayed_retries : NServiceBusAcceptanceTest
{
    [Test]
    [Explicit("Not finished")]
    public async Task Should_retry_the_message()
    {
        var secret = Guid.NewGuid();
        var headers = new Dictionary<string, string>
        {
            ["Secret"] = secret.ToString()
        };
        var body = Encoding.UTF8.GetBytes("Hello world!");

        var result = await Scenario.Define<Context>()
            .WithRawEndpoint("Sender",
                (context, scenario, dispatcher) => Task.CompletedTask,
                (endpoint, scenario) => endpoint.Send("Receiver", headers, body))
            .WithRawEndpoint("Receiver",
                (context, scenario, dispatcher) =>
                {
                    throw new SimulatedException("Boom!");
                }, null,
                cfg =>
                {
                    cfg.CustomErrorHandlingPolicy(new DelayedRetryErrorHandlingPolicy(0, 3, "Receiver.Retries", "ErrorSpy"));
                })
            .WithRawEndpoint("ErrorSpy",
                (context, scenario, dispatcher) => { scenario.MovedToErrorQueue = true; return Task.CompletedTask; })
            .Done(c => c.MessageReceived)
            .Run();

        Assert.IsTrue(result.MessageReceived);
        Assert.AreEqual("Hello world!", result.Message);
    }

    class Context : ScenarioContext
    {
        public bool MessageReceived { get; set; }
        public string Message { get; set; }
        public bool MovedToErrorQueue { get; set; }
    }
}