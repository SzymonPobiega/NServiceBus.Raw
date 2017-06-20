namespace ServiceControl.TransportAdapter.AcceptanceTests.Infrastructure
{
    using System;
    using System.Collections.Generic;
    using System.Messaging;
    using System.Threading.Tasks;
    using NUnit.Framework;

    public abstract class AcceptanceTestBase
    {
        [SetUp]
        public Task ClearQueues()
        {
            return Cleanup();
        }

        Task Cleanup()
        {
            var allQueues = MessageQueue.GetPrivateQueuesByMachine("localhost");
            var queuesToBeDeleted = new List<string>();

            foreach (var messageQueue in allQueues)
            {
                using (messageQueue)
                {

                    if (messageQueue.QueueName.StartsWith(@"private$\SCTA.", StringComparison.OrdinalIgnoreCase))
                    {
                        queuesToBeDeleted.Add(messageQueue.Path);
                    }
                }
            }

            foreach (var queuePath in queuesToBeDeleted)
            {
                try
                {
                    MessageQueue.Delete(queuePath);
                    Console.WriteLine("Deleted '{0}' queue", queuePath);
                }
                catch (Exception)
                {
                    Console.WriteLine("Could not delete queue '{0}'", queuePath);
                }
            }

            MessageQueue.ClearConnectionCache();

            return Task.FromResult(0);
        }
    }
}