using System;
using System.Threading;
using System.Threading.Tasks;
using NServiceBus;
using NServiceBus.AcceptanceTesting;
using NServiceBus.AcceptanceTesting.Support;
using NServiceBus.Raw.DelayedRetries;
using NServiceBus.Transport;

static class DelayedRetryEndpointComponentExtensions
{
    public static IScenarioWithEndpointBehavior<TContext> WithDelayedRetryEndpointComponent<TTransport, TContext>(this IScenarioWithEndpointBehavior<TContext> scenario, Action<TransportExtensions<TTransport>> customizeTransport, string name)
        where TContext : ScenarioContext
        where TTransport : TransportDefinition, new()
    {
        var component = new DelayedRetryEndpointComponent<TTransport, TContext>(name, customizeTransport);
        return scenario.WithComponent(component);
    }
}

class DelayedRetryEndpointComponent<TTransport, TContext> : IComponentBehavior
    where TContext : ScenarioContext
    where TTransport : TransportDefinition, new()
{
    string name;
    Action<TransportExtensions<TTransport>> customizeTransport;

    public DelayedRetryEndpointComponent(string name, Action<TransportExtensions<TTransport>> customizeTransport)
    {
        this.name = name;
        this.customizeTransport = customizeTransport;
    }

    public Task<ComponentRunner> CreateRunner(RunDescriptor run)
    {
        var endpoint = new DelayedRetryEndpoint<TTransport>(name, null, customizeTransport);
        
        return Task.FromResult<ComponentRunner>(new Runner(name, endpoint));
    }

    class Runner : ComponentRunner
    {
        DelayedRetryEndpoint<TTransport> endpoint;

        public Runner(string name, DelayedRetryEndpoint<TTransport> endpoint)
        {
            Name = name;
            this.endpoint = endpoint;
        }

        public override Task ComponentsStarted(CancellationToken token)
        {
            return Task.FromResult(0);
        }

        public override Task Start(CancellationToken token)
        {
            return endpoint.Start();
        }

        public override async Task Stop()
        {
            if (endpoint != null)
            {
                await endpoint.Stop().ConfigureAwait(false);
            }
        }

        public override string Name { get; }
    }
}