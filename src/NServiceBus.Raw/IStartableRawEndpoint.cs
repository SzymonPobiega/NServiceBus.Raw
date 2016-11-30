using System.Threading.Tasks;

namespace NServiceBus.Raw
{
    /// <summary>
    /// Represents an endpoint in the start-up phase.
    /// </summary>
    public interface IStartableRawEndpoint
    {
        /// <summary>
        /// Starts the endpoint and returns a reference to it.
        /// </summary>
        /// <returns>A reference to the endpoint.</returns>
        Task<IRawEndpointInstance> Start();
    }
}