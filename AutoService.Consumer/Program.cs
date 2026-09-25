using AutoService.Consumer.Consumers;
using AutoService.Consumer.Persistence;
using AutoService.Consumer.Projection;
using Microsoft.EntityFrameworkCore;

var builder = Host.CreateApplicationBuilder(args);

var readModelConnection = builder.Configuration.GetConnectionString("ReadModel") ?? "Host=localhost;Port=5432;Database=autoservice_readmodel;Username=postgres;Password=postgres";
builder.Services.AddDbContext<ReadModelDbContext>(options => options.UseNpgsql(readModelConnection));
builder.Services.AddScoped<ServiceOrderProjection>();
builder.Services.AddHostedService<ReadModelWorker>();

var host = builder.Build();
host.Run();
