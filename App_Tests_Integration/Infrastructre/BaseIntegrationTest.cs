namespace App_Tests_Integration.Infrastructre;

[Collection("Database collection")]
public abstract class BaseIntegrationTest : IAsyncLifetime
{
    protected readonly HttpClient Client;
    protected readonly ApiWebApplicationFactory Factory;

    protected BaseIntegrationTest(ApiWebApplicationFactory factory)
    {
        Factory = factory;
        Client = factory.CreateClient();
    }
    
    
    public async Task InitializeAsync()
    {
        await Factory.ResetDatabaseAsync();
    }

    public Task DisposeAsync() => Task.CompletedTask;
}