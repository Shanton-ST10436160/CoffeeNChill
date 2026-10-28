using CoffeeNChill.Functions.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var host = new HostBuilder()
    .ConfigureFunctionsWebApplication()
    .ConfigureServices(services =>
    {
        services.AddSingleton<MenuStorageService>();
        services.AddSingleton<DocumentStorageService>();
        services.AddSingleton<OrderQueueService>();
        services.AddSingleton<OrderTableService>();
    })
    .Build();

host.Run();
