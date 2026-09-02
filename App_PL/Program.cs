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
using App_PL.Exceptions;
using DotNetEnv;
using Microsoft.EntityFrameworkCore;


var builder = WebApplication.CreateBuilder(args);

if (builder.Environment.IsDevelopment())
{
    var envPath = Path.Combine(
        Directory.GetParent(Directory.GetCurrentDirectory())!.FullName,
        ".env");

    if (File.Exists(envPath))
    {
        Env.Load(envPath);
    }
}


var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
                       ?? throw new InvalidOperationException("DefaultConnection is not configured. Check your .env file or environment variables.");

builder.Services.AddOpenApi();
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddProblemDetails();

builder.Services.AddDbContext<AppDbContext>(options => options.UseSqlServer(connectionString));

builder.Services.AddScoped<IBookRepo, BookRepo>();
builder.Services.AddScoped<IBookService, BookService>();

builder.Services.AddScoped<IAuthorRepo, AuthorRepo>();
builder.Services.AddScoped<IAuthorService, AuthorService>();

builder.Services.AddScoped<IUserRepo, UserRepo>();
builder.Services.AddScoped<IUserService, UserService>();

builder.Services.AddScoped<ILoanRepo, LoanRepo>();
builder.Services.AddScoped<ILoanService, LoanService>();

builder.Services.AddAutoMapper(x => x.AddProfile(new DomainProfile()));

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

app.UseExceptionHandler();
app.UseHttpsRedirection();
app.UseRouting();
app.UseAuthorization();


app.MapControllers();


app.Run();

public partial class Program;

