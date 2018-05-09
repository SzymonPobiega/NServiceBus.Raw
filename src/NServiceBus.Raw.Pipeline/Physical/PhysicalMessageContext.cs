namespace NServiceBus.Raw.Pipeline.Physical
{
    using System.Threading.Tasks;
    using Transport;

    public class PhysicalMessageContext : PipeContext
    {
        protected PhysicalMessageContext(IncomingMessage receivedMessage, TransportTransaction transportTransaction, IDispatchMessages dispatcher, RootContext parentContext) 
            : base(parentContext)
        {
            Set(receivedMessage);
            Set(transportTransaction);
            Set(dispatcher);

            Message = receivedMessage;
            Dispatcher = dispatcher;
        }

        public IncomingMessage Message { get; }
        public IDispatchMessages Dispatcher { get; }

        public Task Send(TransportOperations ops)
        {
            return Dispatcher.Dispatch(ops, Get<TransportTransaction>(), this);
        }
    }
}