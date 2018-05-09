namespace NServiceBus.Raw.Pipeline
{
    using System;
    using System.Threading.Tasks;

    /// <summary>
    /// Marks the inner most behavior of the pipeline.
    /// </summary>
    /// <typeparam name="T">The pipeline context type to terminate.</typeparam>
    public abstract class PipelineTerminator<T> : IPipe<T, PipelineTerminator<T>.ITerminatingContext>, IPipelineTerminator where T : IPipeContext
    {
        /// <summary>
        /// This method will be the final one to be called before the pipeline starts to traverse back up the "stack".
        /// </summary>
        /// <param name="context">The current context.</param>
        protected abstract Task Terminate(T context);

        /// <summary>
        /// Invokes the terminate method.
        /// </summary>
        /// <param name="context">Context object.</param>
        /// <param name="next">Ignored since there by definition is no next behavior to call.</param>
        public Task Invoke(T context, Func<ITerminatingContext, Task> next)
        {
            return Terminate(context);
        }

        /// <summary>
        /// A well-known context that terminates the pipeline.
        /// </summary>
        public interface ITerminatingContext : IPipeContext
        {
        }
    }

    interface IPipelineTerminator
    {
    }
}