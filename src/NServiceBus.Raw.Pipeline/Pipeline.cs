namespace NServiceBus.Raw.Pipeline
{
    public class Pipeline
    {
        readonly Module[] modules;

        public Pipeline(Module[] modules)
        {
            this.modules = modules;
        }

        public void Build()
        {
            foreach (var module in modules)
            {
                var context = new ModuleContext();
                module.Attach(context);
            }
        }

        
    }
}