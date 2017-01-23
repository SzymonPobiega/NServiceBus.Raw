//using System;
//using System.Threading.Tasks;

//namespace NServiceBus.Raw.ExactlyOnceSending
//{
//    using System.Data.SqlClient;
//    using System.Transactions;
//    using Transport;

//    public class A
//    {
//        public void d()
//        {
//            Func<MessageContext, IDispatchMessages, Task> onMessage = (context, dispatcher) => Task.FromResult(0);
//            var settings = new ExactlyOnceProcessingSettings(x => x.MessageId, () => new SqlConnection());
//            var config = RawEndpointConfiguration.Create("", onMessage.EnforceExactlyOnceProcessing(settings), "error");
//        }
//    }

//    public class ExactlyOnceProcessingSettings
//    {
//        public Func<MessageContext, string> UniqueGenerator { get; }
//        public Func<SqlConnection> ConnectionBuilder { get; }

//        public ExactlyOnceProcessingSettings(Func<MessageContext, string> uniqueGenerator, Func<SqlConnection> connectionBuilder)
//        {
//            this.UniqueGenerator = uniqueGenerator;
//            ConnectionBuilder = connectionBuilder;
//        }
//    }


//    public static class EnforceExactlyOnceProcessingExtensions
//    {
//        public static Func<MessageContext, IDispatchMessages, Task> EnforceExactlyOnceProcessing(this Func<MessageContext, IDispatchMessages, Task> onMessage, ExactlyOnceProcessingSettings settings)
//        {
//            return (context, dispatcher) =>
//            {
//                return Wrap(settings, context, dispatcher, onMessage);
//            };
//        }

//        static async Task Wrap(ExactlyOnceProcessingSettings settings, MessageContext context, IDispatchMessages dispatcher, Func<MessageContext, IDispatchMessages, Task> onMessage)
//        {
//            Transaction ambientTransaction;
//            SqlConnection connection;
//            SqlTransaction transaction;
//            context.TransportTransaction.TryGet(out ambientTransaction);
//            context.TransportTransaction.TryGet(out connection);
//            context.TransportTransaction.TryGet(out transaction);
//            if (ambientTransaction == null && transaction == null)
//            {
//                throw new Exception("No ambient or SQL transaction on the message context.");
//            }

//            var uniqueId = settings.UniqueGenerator(context);

//            if (await IsProcessed(uniqueId, connection, transaction, settings.ConnectionBuilder).ConfigureAwait(false))
//            {
//                return;
//            }
//            await onMessage(context, dispatcher).ConfigureAwait(false);
//            await MarkProcessed(uniqueId, connection, transaction, settings.ConnectionBuilder).ConfigureAwait(false);
//        }

//        static async Task<bool> IsProcessed(string id, SqlConnection connection, SqlTransaction transaction, Func<SqlConnection> connectionBuilder)
//        {
//            if (connection == null) //In this case there is an ambient transaction
//            {
//                using (connection = connectionBuilder())
//                {
//                    await connection.OpenAsync().ConfigureAwait(false);
//                    return await QueryProcessed(id, connection, null);
//                }
//            }
//            return await QueryProcessed(id, connection, transaction);
//        }

//        static async Task<bool> QueryProcessed(string id, SqlConnection connection, SqlTransaction transaction)
//        {
//            var command = new SqlCommand("SELECT COUNT(*) FROM ProcessedMessages WHERE [UniqueId] = @UniqueId", connection, transaction);
//            command.Parameters.AddWithValue("@UniqueId", id);

//            var count = (int) await command.ExecuteScalarAsync().ConfigureAwait(false);
//            return count > 0;
//        }

//        static Task MarkProcessed(string id, SqlConnection connection, SqlTransaction transaction, Func<SqlConnection> connectionBuilder)
//        {
//            throw new NotImplementedException();
//        }
//    }
//}
