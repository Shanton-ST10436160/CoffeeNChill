using CoffeeNChill.Functions.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var host = new HostBuilder()
    .ConfigureFunctionsWebApplication()
    .ConfigureServices(services =>
    {
        services.AddSingleton<MenuStorageService>();
        services.AddSingleton<DocumentStorageService>();
    })
    .Build();

host.Run();
