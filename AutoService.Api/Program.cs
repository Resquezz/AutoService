using AutoService.Api.Application.Services;
using AutoService.Api.Infrastructure.EventStore;
using AutoService.Api.Infrastructure.Persistence;
using AutoService.Api.Infrastructure.RabbitMQ;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var eventStoreConnection = builder.Configuration.GetConnectionString("EventStore") ?? "Host=localhost;Port=5432;Database=autoservice_eventstore;Username=postgres;Password=postgres";
builder.Services.AddDbContext<EventStoreDbContext>(options => options.UseNpgsql(eventStoreConnection));

builder.Services.AddSingleton<RabbitMqPublisher>();
builder.Services.AddScoped<IEventStore, PostgresEventStore>();
builder.Services.AddScoped<OrderCommandService>();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<EventStoreDbContext>();
    db.Database.EnsureCreated();
}

if (app.Environment.IsDevelopment() || app.Environment.EnvironmentName == "Docker")
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.MapControllers();

app.Run();
