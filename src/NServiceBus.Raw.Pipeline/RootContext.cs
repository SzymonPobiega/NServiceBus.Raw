namespace NServiceBus.Raw.Pipeline
{
    public class RootContext : PipeContext
    {
        public RootContext(IPipeline pipeline) 
            : base(null)
        {
            Set(pipeline);
        }
    }

    public interface IPipeline
    {
        
    }
}