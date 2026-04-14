using AzureSummary.Commands;
using AzureSummary.Display;
using AzureSummary.Infrastructure;
using AzureSummary.Services;
using Microsoft.Extensions.DependencyInjection;
using Spectre.Console.Cli;

var services = new ServiceCollection();
services.AddSingleton<IConfigurationService, ConfigurationService>();
services.AddSingleton<AzureDevOpsHttpClientFactory>();
services.AddSingleton<IAzureDevOpsService, AzureDevOpsService>();
services.AddSingleton<PollingEngine>();
services.AddSingleton<LiveTableRenderer>();

var registrar = new TypeRegistrar(services);
var app = new CommandApp(registrar);

app.Configure(config =>
{
    config.SetApplicationName("azmon");

    config.AddCommand<AddCommand>("add")
        .WithDescription("Add a pipeline to monitor");

    config.AddCommand<RemoveCommand>("remove")
        .WithDescription("Remove a monitored pipeline");

    config.AddCommand<ListCommand>("list")
        .WithDescription("List all monitored pipelines");

    config.AddCommand<ConfigCommand>("config")
        .WithDescription("Configure global settings (PAT, polling interval)");

    config.AddCommand<WatchCommand>("watch")
        .WithDescription("Start live monitoring dashboard");

    config.AddCommand<ColumnsCommand>("columns")
        .WithDescription("Show or configure visible columns in the dashboard");
});

return app.Run(args);

// DI integration for Spectre.Console.Cli
sealed class TypeRegistrar(IServiceCollection services) : ITypeRegistrar
{
    private readonly IServiceCollection _services = services;

    public ITypeResolver Build()
    {
        return new TypeResolver(_services.BuildServiceProvider());
    }

    public void Register(Type service, Type implementation)
        => _services.AddSingleton(service, implementation);

    public void RegisterInstance(Type service, object implementation)
        => _services.AddSingleton(service, implementation);

    public void RegisterLazy(Type service, Func<object> factory)
        => _services.AddSingleton(service, _ => factory());
}

sealed class TypeResolver(IServiceProvider provider) : ITypeResolver, IDisposable
{
    private readonly IServiceProvider _provider = provider;

    public object? Resolve(Type? type)
        => type is null ? null : _provider.GetService(type);

    public void Dispose()
    {
        if (_provider is IDisposable disposable)
            disposable.Dispose();
    }
}
