using Orleans.TestingHost;

namespace OrleansContrib.Tester.Reminders;

public abstract class BaseReminderTestClusterFixture : BaseTestClusterFixture
{
    protected override void ConfigureTestCluster(TestClusterBuilder builder)
    {
        // Application parts: just reference one of the grain implementations that we use
        builder.AddSiloBuilderConfigurator<ReminderSiloBuilderConfiguration>();
        builder.AddClientBuilderConfigurator<ReminderClientBuilderConfiguration>();
        
        ConfigureReminderTestCluster(builder);
    }
    

    protected virtual void ConfigureReminderTestCluster(TestClusterBuilder builder)
    {
    }
}