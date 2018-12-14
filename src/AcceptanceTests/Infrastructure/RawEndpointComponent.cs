using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using NServiceBus;
using NServiceBus.AcceptanceTesting;
using NServiceBus.AcceptanceTesting.Support;
using NServiceBus.Extensibility;
using NServiceBus.Raw;
using NServiceBus.Routing;
using NServiceBus.Transport;

static class RawEndpointComponentExtensions
{
    public static IScenarioWithEndpointBehavior<TContext> WithRawEndpoint<TTransport, TContext>(this IScenarioWithEndpointBehavior<TContext> scenario,
        Action<TransportExtensions<TTransport>> customizeTransport,
        string name, Func<MessageContext, TContext, IDispatchMessages, Task> onMessage,
        Func<IRawEndpoint, TContext, Task> onStarting = null,
        Func<IRawEndpoint, TContext, Task> onStarted = null,
        Action<RawEndpointConfiguration> configure = null)
        where TContext : ScenarioContext
        where TTransport : TransportDefinition, new()
    {
        void configureWithTransport(RawEndpointConfiguration cfg)
        {
            configure?.Invoke(cfg);
            var ext = cfg.UseTransport<TTransport>();
            customizeTransport(ext);
        }

        var component = new RawEndpointComponent<TContext>(name, onMessage, onStarting, onStarted, configureWithTransport);
        return scenario.WithComponent(component);
    }

    public static IScenarioWithEndpointBehavior<TContext> WithRawSendOnlyEndpoint<TTransport, TContext>(this IScenarioWithEndpointBehavior<TContext> scenario,
        Action<TransportExtensions<TTransport>> customizeTransport,
        string name, 
        Func<IRawEndpoint, TContext, Task> onStarting = null,
        Func<IRawEndpoint, TContext, Task> onStarted = null,
        Action<RawEndpointConfiguration> configure = null)
        where TContext : ScenarioContext
        where TTransport : TransportDefinition, new()
    {
        void configureWithTransport(RawEndpointConfiguration cfg)
        {
            configure?.Invoke(cfg);
            var ext = cfg.UseTransport<TTransport>();
            customizeTransport(ext);
        }

        var component = new RawEndpointComponent<TContext>(name, null, onStarting, onStarted, configureWithTransport);
        return scenario.WithComponent(component);
    }

    public static Task Send(this IRawEndpoint endpoint, string destination, Dictionary<string, string> headers, byte[] body)
    {
        var op = new TransportOperation(new OutgoingMessage(Guid.NewGuid().ToString(), headers, body), new UnicastAddressTag(destination));
        return endpoint.Dispatch(new TransportOperations(op), new TransportTransaction(), new ContextBag());
    }

    public static Task Publish(this IRawEndpoint endpoint, Type eventType, Dictionary<string, string> headers, byte[] body)
    {
        var op = new TransportOperation(new OutgoingMessage(Guid.NewGuid().ToString(), headers, body), new MulticastAddressTag(eventType));
        return endpoint.Dispatch(new TransportOperations(op), new TransportTransaction(), new ContextBag());
    }
}

class RawEndpointComponent<TContext> : IComponentBehavior
    where TContext : ScenarioContext
{
    string name;
    Func<MessageContext, TContext, IDispatchMessages, Task> onMessage;
    Func<IRawEndpoint, TContext, Task> onStarting;
    Func<IRawEndpoint, TContext, Task> onStarted;
    Action<RawEndpointConfiguration> configure;

    public RawEndpointComponent(string name, Func<MessageContext, TContext, IDispatchMessages, Task> onMessage, Func<IRawEndpoint, TContext, Task> onStarting, Func<IRawEndpoint, TContext, Task> onStarted, Action<RawEndpointConfiguration> configure)
    {
        this.name = name;
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
            ? RawEndpointConfiguration.CreateSendOnly(name)
            : RawEndpointConfiguration.Create(name, (c, d) => onMessage(c, typedScenarioContext, d), "poison");

        config.AutoCreateQueue();
        configure(config);
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