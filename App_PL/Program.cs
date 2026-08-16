using App_BLL.Mapper.AutoMapper;
using App_DAL.Repos.Abstraction;
using App_DAL.Repos.Implementaion;
using App_PL.Exceptions;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddProblemDetails();

builder.Services.AddSingleton<IBookRepo, InMemoryBookRepo>();
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
}

app.UseExceptionHandler();
app.UseHttpsRedirection();
app.UseRouting();
app.UseAuthorization();


app.MapControllers();


app.Run();

