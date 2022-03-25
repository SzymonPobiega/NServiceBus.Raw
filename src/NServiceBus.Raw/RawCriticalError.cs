using NServiceBus.Logging;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace NServiceBus.Raw
{
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

        static Task DefaultCriticalErrorHandling(ICriticalErrorContext criticalErrorContext, CancellationToken cancellationToken = default(CancellationToken))
        {
            return criticalErrorContext.Stop(CancellationToken.None);
        }

        public override void Raise(string errorMessage, Exception exception, CancellationToken cancellationToken = default(CancellationToken))
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
            RaiseForEndpoint(errorMessage, exception);
        }

        void RaiseForEndpoint(string errorMessage, Exception exception)
        {
            Task.Run(() =>
            {
                var context = new CriticalErrorContext(async (_) =>
                {
                    var stoppable = await endpoint.StopReceiving().ConfigureAwait(false);
                    await stoppable.Stop().ConfigureAwait(false);
                }, errorMessage, exception);
                return criticalErrorAction(context, CancellationToken.None);
            });
        }

        internal void SetEndpoint(IReceivingRawEndpoint endpointInstance)
        {
            lock (endpointCriticalLock)
            {
                endpoint = endpointInstance;
                foreach (var latentCritical in criticalErrors)
                {
                    RaiseForEndpoint(latentCritical.Message, latentCritical.Exception);
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