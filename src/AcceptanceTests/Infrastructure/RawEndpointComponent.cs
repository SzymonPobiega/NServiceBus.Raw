using NServiceBus.AcceptanceTesting;
using NServiceBus.AcceptanceTesting.Support;
using NServiceBus.Raw;
using NServiceBus.Routing;
using NServiceBus.Transport;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

static class RawEndpointComponentExtensions
{
    public static IScenarioWithEndpointBehavior<TContext> WithRawEndpoint<TTransport, TContext>(this IScenarioWithEndpointBehavior<TContext> scenario,
        TransportDefinition transportDefinition,
        string name,
        Func<MessageContext, TContext, IMessageDispatcher, Task> onMessage,
        Func<IRawEndpoint, TContext, Task> onStarting = null,
        Func<IRawEndpoint, TContext, Task> onStarted = null,
        Action<RawEndpointConfiguration> configure = null)
        where TContext : ScenarioContext
        where TTransport : TransportDefinition, new()
    {
        var component = new RawEndpointComponent<TContext>(name, transportDefinition, onMessage, onStarting, onStarted, configure);
        return scenario.WithComponent(component);
    }

    public static IScenarioWithEndpointBehavior<TContext> WithRawSendOnlyEndpoint<TTransport, TContext>(this IScenarioWithEndpointBehavior<TContext> scenario,
        TransportDefinition transportDefinition,
        string name,
        Func<IRawEndpoint, TContext, Task> onStarting = null,
        Func<IRawEndpoint, TContext, Task> onStarted = null,
        Action<RawEndpointConfiguration> configure = null)
        where TContext : ScenarioContext
        where TTransport : TransportDefinition, new()
    {
        var component = new RawEndpointComponent<TContext>(name, transportDefinition, null, onStarting, onStarted, configure);
        return scenario.WithComponent(component);
    }

    public static Task Send(this IRawEndpoint endpoint, string destination, Dictionary<string, string> headers, byte[] body)
    {
        var op = new TransportOperation(new OutgoingMessage(Guid.NewGuid().ToString(), headers, body), new UnicastAddressTag(destination));
        return endpoint.Dispatch(new TransportOperations(op), new TransportTransaction());
    }

    public static Task Publish(this IRawEndpoint endpoint, Type eventType, Dictionary<string, string> headers, byte[] body)
    {
        var op = new TransportOperation(new OutgoingMessage(Guid.NewGuid().ToString(), headers, body), new MulticastAddressTag(eventType));
        return endpoint.Dispatch(new TransportOperations(op), new TransportTransaction());
    }
}

class RawEndpointComponent<TContext> : IComponentBehavior
    where TContext : ScenarioContext
{
    string name;
    private readonly TransportDefinition transportDefinition;
    Func<MessageContext, TContext, IMessageDispatcher, Task> onMessage;
    Func<IRawEndpoint, TContext, Task> onStarting;
    Func<IRawEndpoint, TContext, Task> onStarted;
    Action<RawEndpointConfiguration> configure;

    public RawEndpointComponent(string name, TransportDefinition transportDefinition, Func<MessageContext, TContext, IMessageDispatcher, Task> onMessage, Func<IRawEndpoint, TContext, Task> onStarting, Func<IRawEndpoint, TContext, Task> onStarted, Action<RawEndpointConfiguration> configure)
    {
        this.name = name;
        this.transportDefinition = transportDefinition;
        this.onMessage = onMessage;
        this.onStarting = onStarting;
        this.onStarted = onStarted;
        this.configure = configure;
    }

    public Task<ComponentRunner> CreateRunner(RunDescriptor run)
    {
        var typedScenarioContext = (TContext)run.ScenarioContext;

        var sendOnly = onMessage == null;
        var config = sendOnly
            ? RawEndpointConfiguration.CreateSendOnly(name, transportDefinition)
            : RawEndpointConfiguration.Create(name, transportDefinition, (c, d) => onMessage(c, typedScenarioContext, d), "poison");

        config.AutoCreateQueues();

        if (configure != null)
        {
            configure(config);
        }

        return Task.FromResult<ComponentRunner>(new Runner(config, name, sendOnly,
            endpoint => onStarting != null ? onStarting(endpoint, typedScenarioContext) : Task.FromResult(0),
            endpoint => onStarted != null ? onStarted(endpoint, typedScenarioContext) : Task.FromResult(0)));
    }

    class Runner : ComponentRunner
    {
        RawEndpointConfiguration config;
        bool sendOnly;
        IRawEndpoint endpoint;
        Func<IRawEndpoint, Task> onStarting;
        Func<IRawEndpoint, Task> onStarted;

        public Runner(RawEndpointConfiguration config, string name, bool sendOnly, Func<IRawEndpoint, Task> onStarting, Func<IRawEndpoint, Task> onStarted)
        {
            this.config = config;
            this.sendOnly = sendOnly;
            this.onStarting = onStarting;
            Name = name;
            this.onStarted = onStarted;
        }

        public override async Task ComponentsStarted(CancellationToken token)
        {
            await onStarted(endpoint).ConfigureAwait(false);
        }

        public override async Task Start(CancellationToken token)
        {
            var startable = await RawEndpoint.Create(config).ConfigureAwait(false);
            if (!sendOnly)
            {
                endpoint = await startable.Start().ConfigureAwait(false);
            }
            else
            {
                endpoint = startable;
            }

            await onStarting(endpoint).ConfigureAwait(false);
        }

        public override async Task Stop()
        {
            if (endpoint is IStoppableRawEndpoint stoppable)
            {
                await stoppable.Stop().ConfigureAwait(false);
            }
        }

        public override string Name { get; }
    }
}