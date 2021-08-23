using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using NServiceBus.Logging;

namespace NServiceBus.Raw
{
    using System.Threading;

    class RawCriticalError : CriticalError
    {
        public RawCriticalError(Func<ICriticalErrorContext, CancellationToken, Task> onCriticalErrorAction)
            : base(onCriticalErrorAction)
        {
            if (onCriticalErrorAction == null)
            {
                criticalErrorAction = DefaultCriticalErrorHandling;
            }
            else
            {
                criticalErrorAction = onCriticalErrorAction;
            }
        }

        static Task DefaultCriticalErrorHandling(ICriticalErrorContext criticalErrorContext, CancellationToken cancellationToken = default)
        {
            return criticalErrorContext.Stop(cancellationToken);
        }

        public override void Raise(string errorMessage, Exception exception, CancellationToken cancellationToken)
        {
            //Intentionally don't call base.Raise
            Guard.AgainstNullAndEmpty(nameof(errorMessage), errorMessage);
            Guard.AgainstNull(nameof(exception), exception);
            LogManager.GetLogger("NServiceBus").Fatal(errorMessage, exception);

            lock (endpointCriticalLock)
            {
                if (endpoint == null)
                {
                    criticalErrors.Add(new LatentCritical
                    {
                        Message = errorMessage,
                        Exception = exception
                    });
                    return;
                }
            }

            // don't await the criticalErrorAction in order to avoid deadlocks
            RaiseForEndpoint(errorMessage, exception, cancellationToken);
        }

        void RaiseForEndpoint(string errorMessage, Exception exception, CancellationToken cancellationToken)
        {
            Task.Run(() =>
            {
                var context = new CriticalErrorContext(async ct =>
                {
                    var stoppable = await endpoint.StopReceiving(ct).ConfigureAwait(false);
                    await stoppable.Stop(ct).ConfigureAwait(false);
                }, errorMessage, exception);
                return criticalErrorAction(context, cancellationToken);
            });
        }

        internal void SetEndpoint(IReceivingRawEndpoint endpointInstance, CancellationToken cancellationToken)
        {
            lock (endpointCriticalLock)
            {
                endpoint = endpointInstance;
                foreach (var latentCritical in criticalErrors)
                {
                    RaiseForEndpoint(latentCritical.Message, latentCritical.Exception, cancellationToken);
                }
                criticalErrors.Clear();
            }
        }

        Func<CriticalErrorContext, CancellationToken, Task> criticalErrorAction;

        List<LatentCritical> criticalErrors = new List<LatentCritical>();
        IReceivingRawEndpoint endpoint;
        object endpointCriticalLock = new object();

        class LatentCritical
        {
            public string Message { get; set; }
            public Exception Exception { get; set; }
        }
    }
}