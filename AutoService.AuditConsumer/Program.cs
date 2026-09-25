using AutoService.AuditConsumer.Consumer;

var builder = Host.CreateApplicationBuilder(args);
builder.Services.AddHostedService<AuditWorker>();

var host = builder.Build();
host.Run();
