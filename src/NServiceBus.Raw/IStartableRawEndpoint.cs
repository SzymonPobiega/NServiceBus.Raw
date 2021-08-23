using System.Threading.Tasks;

namespace NServiceBus.Raw
{
    using System.Threading;

    /// <summary>
    /// Represents an endpoint in the start-up phase.
    /// </summary>
    public interface IStartableRawEndpoint : IRawEndpoint
    {
        /// <summary>
        /// Starts the endpoint and returns a reference to it.
        /// </summary>
        /// <returns>A reference to the endpoint.</returns>
        Task<IReceivingRawEndpoint> Start(CancellationToken cancellationToken);
    }
}