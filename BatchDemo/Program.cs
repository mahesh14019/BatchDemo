using BatchDemo.DataAccess;
using BatchDemo.DataAccess.Repository;
using BatchDemo.DataAccess.Repository.IRepository;
using BatchDemo.Logger;
using BatchDemo.Services;
using BatchDemo.Services.Interface;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;
using Serilog;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;

var builder = WebApplication.CreateBuilder(args);
//try
//{
var logger = new LoggerConfiguration()
.ReadFrom.Configuration(builder.Configuration)
.Enrich.FromLogContext()
.CreateLogger();

builder.Logging.ClearProviders();
builder.Logging.AddSerilog(logger);
//}
//finally
//{
//  Log.CloseAndFlush();
//}

builder.Services.AddControllers();
// Register custom defined Correlation ID Generator
//builder.Services.AddCorrelationIdGenerator();
// Disable automatic validate response
builder.Services.Configure<ApiBehaviorOptions>(options =>
{
    options.SuppressModelStateInvalidFilter = true;
});

builder.Services.AddScoped<IKeyVaultManager, KeyVaultManager>();

var dbConnectionString = new KeyVaultManager(builder.Configuration).GetDbConnectionFromAzureVault();//builder.Configuration.Configuration[Configuration[DBConnectionStringSecretIdentifierKey]];
if (string.IsNullOrEmpty(dbConnectionString))
{
    throw new ApplicationException(message: "Failed to get database connection string");
}
builder.Services.AddDbContext<ApplicationDbContext>(options => options.UseSqlServer(dbConnectionString));

//builder.Services.AddDbContext<ApplicationDbContext>(options => options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));
builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();
builder.Services.AddScoped<IBatchUtility, BatchUtility>();
builder.Services.AddScoped<IBatchBlobService, BatchBlobService>();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Version = "v1",
        Title = "Batch Demo API",
        Description = "An ASP.NET Core Web API for creation of batch to upload and get files",
    });
    var xmlFilename = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
    options.IncludeXmlComments(Path.Combine(AppContext.BaseDirectory, xmlFilename));
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthorization();
// Register for exception handling. Order is important to call
//app.ConfigureExceptionHandler(logger);
app.UseGlobalExceptionMiddleware(logger);
// Add Correlation ID Middleware
// app.AddCorrelationIdMiddleware();
app.MapControllers();
app.Run();

/// <summary>
/// 
/// </summary>
[ExcludeFromCodeCoverage]
public partial class Program { }