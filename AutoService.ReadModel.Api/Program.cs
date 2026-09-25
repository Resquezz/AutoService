using AutoService.ReadModel.Api.Persistence;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var readModelConnection = builder.Configuration.GetConnectionString("ReadModel") ?? "Host=localhost;Port=5432;Database=autoservice_readmodel;Username=postgres;Password=postgres";
builder.Services.AddDbContext<ReadModelDbContext>(options => options.UseNpgsql(readModelConnection));

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ReadModelDbContext>();
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
