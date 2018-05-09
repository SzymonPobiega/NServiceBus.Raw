namespace NServiceBus.Raw.Pipeline
{
    using Extensibility;

    public abstract class PipeContext : ContextBag, IPipeContext
    {
        protected PipeContext(IPipeContext parentContext) : base(parentContext?.Extensions)
        {
        }

        public ContextBag Extensions => this;
    }
}