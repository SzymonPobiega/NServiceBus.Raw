using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using NServiceBus.Logging;

namespace NServiceBus.Raw
{
    class RawCriticalError : CriticalError
    {
        public RawCriticalError(Func<ICriticalErrorContext, Task> onCriticalErrorAction)
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

        static Task DefaultCriticalErrorHandling(ICriticalErrorContext criticalErrorContext)
        {
            return criticalErrorContext.Stop();
        }

        public override void Raise(string errorMessage, Exception exception)
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
                var context = new CriticalErrorContext(endpoint.Stop, errorMessage, exception);
                return criticalErrorAction(context);
            });
        }

        internal void SetEndpoint(IRawEndpointInstance endpointInstance)
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

        Func<CriticalErrorContext, Task> criticalErrorAction;

        List<LatentCritical> criticalErrors = new List<LatentCritical>();
        IRawEndpointInstance endpoint;
        object endpointCriticalLock = new object();

        class LatentCritical
        {
            public string Message { get; set; }
            public Exception Exception { get; set; }
        }
    }
}