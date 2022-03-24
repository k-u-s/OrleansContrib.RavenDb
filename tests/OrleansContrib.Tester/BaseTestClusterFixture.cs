using System;
using System.Runtime.ExceptionServices;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Orleans;
using Orleans.Configuration;
using Orleans.TestingHost;

namespace OrleansContrib.Tester;


public abstract class BaseTestClusterFixture : Xunit.IAsyncLifetime
{
    private readonly ExceptionDispatchInfo preconditionsException;

    protected BaseTestClusterFixture()
    {
        try
        {
            CheckPreconditionsOrThrow();
        }
        catch (Exception ex)
        {
            preconditionsException = ExceptionDispatchInfo.Capture(ex);
            return;
        }
    }

    public void EnsurePreconditionsMet()
    {
        preconditionsException?.Throw();
    }

    protected virtual void CheckPreconditionsOrThrow()
    {
    }

    protected virtual void ConfigureTestCluster(TestClusterBuilder builder)
    {
    }

    public TestCluster HostedCluster { get; private set; }

    public IGrainFactory GrainFactory => HostedCluster?.GrainFactory;

    public IClusterClient Client => HostedCluster?.Client;

    public ILogger Logger { get; private set; }

    public string GetClientServiceId() =>
        Client.ServiceProvider.GetRequiredService<IOptions<ClusterOptions>>().Value.ServiceId;

    public virtual async Task InitializeAsync()
    {
        EnsurePreconditionsMet();
        var builder = new TestClusterBuilder();
        ConfigureTestCluster(builder);

        var testCluster = builder.Build();
        if (testCluster.Primary == null)
        {
            await testCluster.DeployAsync().ConfigureAwait(false);
        }

        HostedCluster = testCluster;
        Logger = Client.ServiceProvider.GetRequiredService<ILoggerFactory>().CreateLogger("Application");
    }

    public virtual async Task DisposeAsync()
    {
        var cluster = HostedCluster;
        if (cluster is null) return;

        try
        {
            await cluster.StopAllSilosAsync().ConfigureAwait(false);
        }
        finally
        {
            await cluster.DisposeAsync().ConfigureAwait(false);
        }
    }
}