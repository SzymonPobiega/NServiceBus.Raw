using NServiceBus.AcceptanceTesting;
using NServiceBus.AcceptanceTesting.Support;
using NServiceBus.Raw.DelayedRetries;
using NServiceBus.Transport;
using System.Threading;
using System.Threading.Tasks;

static class DelayedRetryEndpointComponentExtensions
{
    public static IScenarioWithEndpointBehavior<TContext> WithDelayedRetryEndpointComponent<TTransport, TContext>(
        this IScenarioWithEndpointBehavior<TContext> scenario,
        TransportDefinition transportDefinition,
        string name)
        where TContext : ScenarioContext
        where TTransport : TransportDefinition
    {
        var component = new DelayedRetryEndpointComponent<TTransport, TContext>(name, transportDefinition);
        return scenario.WithComponent(component);
    }
}

class DelayedRetryEndpointComponent<TTransport, TContext> : IComponentBehavior
    where TContext : ScenarioContext
    where TTransport : TransportDefinition
{
    string name;
    TransportDefinition transportDefinition;

    public DelayedRetryEndpointComponent(string name, TransportDefinition transportDefinition)
    {
        this.name = name;
        this.transportDefinition = transportDefinition;
    }

    public Task<ComponentRunner> CreateRunner(RunDescriptor run)
    {
        var endpoint = new DelayedRetryEndpoint(transportDefinition, name, null);

        return Task.FromResult<ComponentRunner>(new Runner(name, endpoint));
    }

    class Runner : ComponentRunner
    {
        DelayedRetryEndpoint endpoint;

        public Runner(string name, DelayedRetryEndpoint endpoint)
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