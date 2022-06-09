using NServiceBus;
using NUnit.Framework;

[TestFixture]
public class When_pausing_and_resuming_SqlServer : When_pausing_and_resuming<SqlServerTransport>
{
    protected override void SetupTransport(TransportExtensions<SqlServerTransport> extensions)
    {
        extensions.ConfigureSql();
    }
}