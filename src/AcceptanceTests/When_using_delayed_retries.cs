using NServiceBus.AcceptanceTesting;
using NServiceBus.AcceptanceTests;
using NServiceBus.Raw.DelayedRetries;
using NServiceBus.Transport;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

public abstract class When_using_delayed_retries<TTransport> : NServiceBusAcceptanceTest<TTransport>
    where TTransport : TransportDefinition, new()
{
    [Test]
    public async Task It_should_retry_the_message_configured_number_of_times()
    {
        var headers = new Dictionary<string, string>();
        var body = Encoding.UTF8.GetBytes("Hello world!");

        var result = await Scenario.Define<Context>()
            .WithRawEndpoint<TTransport, Context>(SetupTransport(), "Endpoint",
                onMessage: (context, scenario, dispatcher) =>
                 {
                     scenario.Attempts++;
                     throw new Exception("Boom!");
                 },
                onStarted: (endpoint, scenario) => endpoint.Send("Endpoint", headers, body),
                configure: config =>
                {
                    config.LimitMessageProcessingConcurrencyTo(1);
                    config.CustomErrorHandlingPolicy(new DelayedRetryErrorHandlingPolicy(0, 5, "DelayedRetries", "FailureSpy", TimeSpan.FromMilliseconds(100)));
                })
            .WithDelayedRetryEndpointComponent<TTransport, Context>(SetupTransport(), "DelayedRetries")
            .WithRawEndpoint<TTransport, Context>(SetupTransport(), "FailureSpy",
                onMessage: (context, scenario, dispatcher) =>
                {
                    scenario.MessageMovedToErrorQueue = true;
                    return Task.FromResult(0);
                })
            .Done(c => c.MessageMovedToErrorQueue)
            .Run();

        Assert.IsTrue(result.MessageMovedToErrorQueue);
        Assert.AreEqual(5, result.Attempts);
    }

    class Context : ScenarioContext
    {
        public int Attempts { get; set; }
        public bool MessageMovedToErrorQueue { get; set; }
    }
}