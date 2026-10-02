// Path: Skyscreen.Server/Program.cs

using Skyscreen.Server;
using Skyscreen.Server.Services;

HostApplicationBuilder builder = Host.CreateApplicationBuilder(args);

builder.Services.AddSingleton<ClientSessionManager>();
builder.Services.AddHostedService<Worker>();

IHost host = builder.Build();

host.Run();