using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using NServiceBus.AcceptanceTesting;
using NServiceBus.AcceptanceTests;
using NUnit.Framework;

[TestFixture]
public class When_sending_to_another_endpoint : NServiceBusAcceptanceTest
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
            .WithRawEndpoint("Sender",
                (context, scenario, dispatcher) => Task.CompletedTask,
                (endpoint, scenario) => endpoint.Send("Receiver", headers, body))
            .WithRawEndpoint("Receiver",
                (context, scenario, dispatcher) =>
                {
                    if (context.Headers.TryGetValue("Secret", out string receivedSecret) && receivedSecret == secret.ToString())
                    {
                        scenario.MessageReceived = true;
                        scenario.Message = Encoding.UTF8.GetString(context.Body);
                    }
                    return Task.CompletedTask;
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