using App_BLL.Mapper.AutoMapper;
using App_BLL.Services.Abstraction.Authors;
using App_BLL.Services.Abstraction.Books;
using App_BLL.Services.Abstraction.Loans;
using App_BLL.Services.Abstraction.Users;
using App_BLL.Services.Implementation.Authors;
using App_BLL.Services.Implementation.Books;
using App_BLL.Services.Implementation.Loans;
using App_BLL.Services.Implementation.Users;
using App_DAL.Database;
using App_DAL.Repos.Abstraction.Authors;
using App_DAL.Repos.Abstraction.Books;
using App_DAL.Repos.Abstraction.Loans;
using App_DAL.Repos.Abstraction.Users;
using App_DAL.Repos.Implementation.Authors;
using App_DAL.Repos.Implementation.Books;
using App_DAL.Repos.Implementation.Loans;
using App_DAL.Repos.Implementation.Users;
using App_PL.ConfigValidators.Database;
using App_PL.Exceptions;
using App_PL.Middlewares.loggingMiddleware;
using DotNetEnv;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Serilog;

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateBootstrapLogger();

try
{
    Log.Information("Starting up");

    var builder = WebApplication.CreateBuilder(args);

    if (builder.Environment.IsDevelopment())
    {
        var envPath = Path.Combine(
            Directory.GetParent(Directory.GetCurrentDirectory())!.FullName,
            ".env");

        if (File.Exists(envPath))
        {
            Env.Load(envPath);
            builder.Configuration.AddEnvironmentVariables();
        }
    }


    builder.Host.UseSerilog((context, services, configuration) => configuration
        .ReadFrom.Configuration(context.Configuration)
        .ReadFrom.Services(services)
        .Enrich.FromLogContext()
        .Enrich.WithMachineName()
        .Enrich.WithProcessId()
        .Enrich.WithThreadId()
        .Enrich.WithProperty("Application", "BookCatalogPlatform"));


    builder.Services.AddSingleton<IValidateOptions<DatabaseOptions>, DatabaseOptionsValidator>();
    builder.Services.AddOptions<DatabaseOptions>()
        .Bind(builder.Configuration.GetSection(DatabaseOptions.SectionName))
        .ValidateOnStart();

    builder.Services.AddOpenApi();
    builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
    builder.Services.AddProblemDetails();

    builder.Services.AddDbContext<AppDbContext>((serviceProvider, options) =>
    {
        var dbOptions = serviceProvider.GetRequiredService<IOptions<DatabaseOptions>>().Value;
        options.UseSqlServer(dbOptions.DefaultConnection);
    });

    builder.Services.AddScoped<IBookRepo, BookRepo>();
    builder.Services.AddScoped<IBookService, BookService>();

    builder.Services.AddScoped<IAuthorRepo, AuthorRepo>();
    builder.Services.AddScoped<IAuthorService, AuthorService>();

    builder.Services.AddScoped<IUserRepo, UserRepo>();
    builder.Services.AddScoped<IUserService, UserService>();

    builder.Services.AddScoped<ILoanRepo, LoanRepo>();
    builder.Services.AddScoped<ILoanService, LoanService>();

    builder.Services.AddAutoMapper(x => x.AddProfile(new DomainProfile()));

    builder.Services.AddHealthChecks()
        .AddDbContextCheck<AppDbContext>(name: "database", tags: new[] { "ready" });

    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerGen();

    builder.Services.AddControllers();

    var app = builder.Build();

    if (app.Environment.IsDevelopment())
    {
        app.MapOpenApi();
        app.UseSwagger();
        app.UseSwaggerUI();
        using var scope = app.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        db.Database.Migrate();
    }


    app.UseMiddleware<CorrelationIdMiddleware>();
    app.UseExceptionHandler();
    app.UseHttpsRedirection();
    app.UseRouting();
    app.UseSerilogRequestLogging();
    app.UseAuthorization();

    app.MapHealthChecks("/health/live", new HealthCheckOptions
    {
        Predicate = _ => false
    });

    app.MapHealthChecks("/health/ready", new HealthCheckOptions
    {
        Predicate = check => check.Tags.Contains("ready")
    });

    app.MapControllers();

    app.Run();
}
catch (Exception ex)
{

    Log.Fatal(ex, "Application terminated unexpectedly");
}
finally
{

    Log.CloseAndFlush();
}

public partial class Program;