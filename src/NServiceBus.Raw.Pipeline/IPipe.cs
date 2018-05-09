namespace NServiceBus.Raw.Pipeline
{
    using System;
    using System.Threading.Tasks;
    using Extensibility;

    public interface IPipe<in TInContext, out TOutContext> : IPipe
        where TInContext : IPipeContext
        where TOutContext : IPipeContext
    {
        Task Invoke(TInContext context, Func<TOutContext, Task> next);
    }

    public interface IPipe
    {
    }

    public interface IPipeContext
    {
        ContextBag Extensions { get; }
    }
}
