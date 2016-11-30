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
        string errorQueue;
        Dictionary<string, string> staticFaultMetadata;
        int immediateRetryCount;

        public RawEndpointErrorHandlingPolicy(ReadOnlySettings settings, IDispatchMessages dispatcher, string errorQueue, int immediateRetryCount)
        {
            this.dispatcher = dispatcher;
            this.errorQueue = errorQueue;
            this.immediateRetryCount = immediateRetryCount;

            staticFaultMetadata = new Dictionary<string, string>
            {
                {FaultsHeaderKeys.FailedQ, settings.LocalAddress()},
                {Headers.ProcessingMachine, RuntimeEnvironment.MachineName},
                {Headers.ProcessingEndpoint, settings.EndpointName()},
                //{Headers.HostId, hostInfo.HostId.ToString("N")},
                //{Headers.HostDisplayName, hostInfo.DisplayName}
            };
        }

        public async Task<ErrorHandleResult> OnError(ErrorContext errorContext)
        {
            if (errorContext.ImmediateProcessingFailures < immediateRetryCount)
            {
                return ErrorHandleResult.RetryRequired;
            }
            await MoveToErrorQueue(errorContext).ConfigureAwait(false);
            return ErrorHandleResult.Handled;
        }

        Task MoveToErrorQueue(ErrorContext errorContext)
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

            return dispatcher.Dispatch(transportOperations, errorContext.TransportTransaction, new ContextBag());
        }
    }
}