using App_DAL.Database;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Respawn;
using Testcontainers.MsSql;

namespace App_Tests_Integration.Infrastructre;

public class ApiWebApplicationFactory : WebApplicationFactory<Program> , IAsyncLifetime
{
    
    private readonly MsSqlContainer _dbContainer = new MsSqlBuilder().WithImage("mcr.microsoft.com/mssql/server:2022-CU20-ubuntu-22.04").Build();
    private Respawner _respawner = null!;
    private string _connectionString = null!;
    
    
    
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureTestServices(services =>
        {
            var descriptor = services.SingleOrDefault(
                d => d.ServiceType == typeof(DbContextOptions<AppDbContext>));

            if (descriptor != null)
            {
                services.Remove(descriptor);
            }
            
            services.AddDbContext<AppDbContext>(options =>
            {
                options.UseSqlServer(_dbContainer.GetConnectionString());
            });
        });
    }
    
    public async Task InitializeAsync()
    {
        await _dbContainer.StartAsync();
        _connectionString = _dbContainer.GetConnectionString();

        using var scope = Services.CreateScope();
        await scope.ServiceProvider
            .GetRequiredService<AppDbContext>()
            .Database
            .MigrateAsync();

        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync();

        _respawner = await Respawner.CreateAsync(connection, new RespawnerOptions
        {
            DbAdapter = DbAdapter.SqlServer
        });
    }

    public async Task ResetDatabaseAsync()
    {
        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync();
        await _respawner.ResetAsync(connection);
    }

    public new async Task DisposeAsync()
    {
        await _dbContainer.DisposeAsync();
    }
}