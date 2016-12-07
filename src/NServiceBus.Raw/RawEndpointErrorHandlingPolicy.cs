using System.Collections.Generic;
using System.Threading.Tasks;
using NServiceBus.Extensibility;
using NServiceBus.Faults;
using NServiceBus.Routing;
using NServiceBus.Settings;
using NServiceBus.Support;
using NServiceBus.Transport;

namespace NServiceBus.Raw
{
    class RawEndpointErrorHandlingPolicy
    {
        IDispatchMessages dispatcher;
        Dictionary<string, string> staticFaultMetadata;
        IErrorHandlingPolicy policy;

        public RawEndpointErrorHandlingPolicy(ReadOnlySettings settings, IDispatchMessages dispatcher, IErrorHandlingPolicy policy)
        {
            this.dispatcher = dispatcher;
            this.policy = policy;

            staticFaultMetadata = new Dictionary<string, string>
            {
                {FaultsHeaderKeys.FailedQ, settings.LocalAddress()},
                {Headers.ProcessingMachine, RuntimeEnvironment.MachineName},
                {Headers.ProcessingEndpoint, settings.EndpointName()},
                //{Headers.HostId, hostInfo.HostId.ToString("N")},
                //{Headers.HostDisplayName, hostInfo.DisplayName}
            };
        }

        public Task<ErrorHandleResult> OnError(ErrorContext errorContext)
        {
            return policy.OnError(errorContext, dispatcher, MoveToErrorQueue);
        }

        async Task<ErrorHandleResult> MoveToErrorQueue(ErrorContext errorContext, string errorQueue)
        {
            var message = errorContext.Message;

            var outgoingMessage = new OutgoingMessage(message.MessageId, new Dictionary<string, string>(message.Headers), message.Body);

            var headers = outgoingMessage.Headers;
            headers.Remove(Headers.DelayedRetries);
            headers.Remove(Headers.ImmediateRetries);

            ExceptionHeaderHelper.SetExceptionHeaders(headers, errorContext.Exception);

            foreach (var faultMetadata in staticFaultMetadata)
            {
                headers[faultMetadata.Key] = faultMetadata.Value;
            }

            var transportOperations = new TransportOperations(new TransportOperation(outgoingMessage, new UnicastAddressTag(errorQueue)));

            await dispatcher.Dispatch(transportOperations, errorContext.TransportTransaction, new ContextBag()).ConfigureAwait(false);
            return ErrorHandleResult.Handled;
        }
    }
}