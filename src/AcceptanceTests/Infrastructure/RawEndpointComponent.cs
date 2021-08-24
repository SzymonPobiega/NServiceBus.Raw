using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using NServiceBus;
using NServiceBus.AcceptanceTesting;
using NServiceBus.AcceptanceTesting.Support;
using NServiceBus.Raw;
using NServiceBus.Routing;
using NServiceBus.Transport;

static class RawEndpointComponentExtensions
{
    public static IScenarioWithEndpointBehavior<TContext> WithRawEndpoint<TContext>(this IScenarioWithEndpointBehavior<TContext> scenario,
        TransportDefinition transport,
        string name, Func<MessageContext, TContext, IMessageDispatcher, Task> onMessage,
        Func<IRawEndpoint, TContext, Task> onStarting = null,
        Func<IRawEndpoint, TContext, Task> onStarted = null,
        Action<RawEndpointConfiguration> configure = null)
        where TContext : ScenarioContext
    {
        void configureWithTransport(RawEndpointConfiguration cfg)
        {
            configure?.Invoke(cfg);
        }

        var component = new RawEndpointComponent<TContext>(name, transport, onMessage, onStarting, onStarted, configureWithTransport);
        return scenario.WithComponent(component);
    }

    public static IScenarioWithEndpointBehavior<TContext> WithRawSendOnlyEndpoint<TContext>(this IScenarioWithEndpointBehavior<TContext> scenario,
        TransportDefinition transport,
        string name,
        Func<IRawEndpoint, TContext, Task> onStarting = null,
        Func<IRawEndpoint, TContext, Task> onStarted = null,
        Action<SendOnlyRawEndpointConfiguration> configure = null)
        where TContext : ScenarioContext
    {
        void configureWithTransport(SendOnlyRawEndpointConfiguration cfg)
        {
            configure?.Invoke(cfg);
        }

        var component = new SendOnlyRawEndpointComponent<TContext>(name, transport, onStarting, onStarted, configureWithTransport);
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
    readonly string name;
    readonly TransportDefinition transport;
    readonly Func<MessageContext, TContext, IMessageDispatcher, Task> onMessage;
    readonly Func<IRawEndpoint, TContext, Task> onStarting;
    readonly Func<IRawEndpoint, TContext, Task> onStarted;
    readonly Action<RawEndpointConfiguration> configure;

    public RawEndpointComponent(string name, TransportDefinition transport, Func<MessageContext, TContext, IMessageDispatcher, Task> onMessage, Func<IRawEndpoint, TContext, Task> onStarting, Func<IRawEndpoint, TContext, Task> onStarted, Action<RawEndpointConfiguration> configure)
    {
        this.name = name;
        this.transport = transport;
        this.onMessage = onMessage;
        this.onStarting = onStarting;
        this.onStarted = onStarted;
        this.configure = configure;
    }

    public Task<ComponentRunner> CreateRunner(RunDescriptor run)
    {
        var typedScenarioContext = (TContext)run.ScenarioContext;

        var config = RawEndpointConfiguration.Create(name, name, transport, (c, d, ct) => onMessage(c, typedScenarioContext, d), "poison");

        config.InfrastructureSetup = InfrastructureSetup.CreateQueues();
        configure(config);
        return Task.FromResult<ComponentRunner>(new Runner(config, name, 
            endpoint => onStarting != null ? onStarting(endpoint, typedScenarioContext) : Task.FromResult(0),
            endpoint => onStarted != null ? onStarted(endpoint, typedScenarioContext) : Task.FromResult(0)));
    }

    class Runner : ComponentRunner
    {
        RawEndpointConfiguration config;
        IRawEndpoint endpoint;
        Func<IRawEndpoint, Task> onStarting;
        Func<IRawEndpoint, Task> onStarted;

        public Runner(RawEndpointConfiguration config, string name, Func<IRawEndpoint, Task> onStarting, Func<IRawEndpoint, Task> onStarted)
        {
            this.config = config;
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
            endpoint = await startable.Start(token).ConfigureAwait(false);

            await onStarting(endpoint).ConfigureAwait(false);
        }

        public override async Task Stop()
        {
            if (endpoint is IStoppableRawEndpoint stoppable)
            {
                await stoppable.Stop(CancellationToken.None).ConfigureAwait(false);
            }
        }

        public override string Name { get; }
    }
}

class SendOnlyRawEndpointComponent<TContext> : IComponentBehavior
    where TContext : ScenarioContext
{
    readonly string name;
    readonly TransportDefinition transport;
    readonly Func<IRawEndpoint, TContext, Task> onStarting;
    readonly Func<IRawEndpoint, TContext, Task> onStarted;
    readonly Action<SendOnlyRawEndpointConfiguration> configure;

    public SendOnlyRawEndpointComponent(string name, TransportDefinition transport, Func<IRawEndpoint, TContext, Task> onStarting, Func<IRawEndpoint, TContext, Task> onStarted, Action<SendOnlyRawEndpointConfiguration> configure)
    {
        this.name = name;
        this.transport = transport;
        this.onStarting = onStarting;
        this.onStarted = onStarted;
        this.configure = configure;
    }

    public Task<ComponentRunner> CreateRunner(RunDescriptor run)
    {
        var typedScenarioContext = (TContext)run.ScenarioContext;

        var config = RawEndpointConfiguration.CreateSendOnly(name, transport);

        configure(config);
        return Task.FromResult<ComponentRunner>(new Runner(config, name,
            endpoint => onStarting != null ? onStarting(endpoint, typedScenarioContext) : Task.FromResult(0),
            endpoint => onStarted != null ? onStarted(endpoint, typedScenarioContext) : Task.FromResult(0)));
    }

    class Runner : ComponentRunner
    {
        SendOnlyRawEndpointConfiguration config;
        IRawEndpoint endpoint;
        Func<IRawEndpoint, Task> onStarting;
        Func<IRawEndpoint, Task> onStarted;

        public Runner(SendOnlyRawEndpointConfiguration config, string name, Func<IRawEndpoint, Task> onStarting, Func<IRawEndpoint, Task> onStarted)
        {
            this.config = config;
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
            endpoint = await RawEndpoint.Create(config).ConfigureAwait(false);
            await onStarting(endpoint).ConfigureAwait(false);
        }

        public override async Task Stop()
        {
            if (endpoint is IStoppableRawEndpoint stoppable)
            {
                await stoppable.Stop(CancellationToken.None).ConfigureAwait(false);
            }
        }

        public override string Name { get; }
    }
}