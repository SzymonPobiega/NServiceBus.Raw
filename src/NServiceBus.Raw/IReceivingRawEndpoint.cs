using System.Threading.Tasks;

namespace NServiceBus.Raw
{
    /// <summary>
    /// Represents an endpoint in the running phase.
    /// </summary>
    public interface IReceivingRawEndpoint : IStoppableRawEndpoint, IRawEndpoint
    {
        /// <summary>
        /// Stops receiving of messages. The endpoint can still send messages.
        /// </summary>
        Task<IStoppableRawEndpoint> StopReceiving();

        /// <summary>
        /// Pauses receiving.
        /// </summary>
        /// <returns></returns>
        Task Pause();

        /// <summary>
        /// Resumes receiving.
        /// </summary>
        /// <returns></returns>
        Task Resume();

        /// <summary>
        /// Resumes receiving with specified maximum concurrency.
        /// </summary>
        /// <returns></returns>
        Task Resume(int maximumConcurrency);
    }
}