namespace NServiceBus.Raw.Pipeline
{
    public abstract class Module
    {
        public abstract void Attach(ModuleContext context);
    }
}