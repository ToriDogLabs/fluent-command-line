using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.CommandLine;
using System.CommandLine.Builder;
using System.CommandLine.Parsing;

namespace FluentCommandLine;

public static class FluentCommandLineInitializer
{
	public static void AddFluentCommandLineServices(this IServiceCollection services)
	{
		services.TryAddScoped(sp => sp.GetRequiredService<InvocationContextProvider>().InvocationContext);
		services.TryAddScoped(sp => sp.GetRequiredService<InvocationContextProvider>().InvocationContext.Console);
		services.TryAddScoped(sp => sp.GetRequiredService<InvocationContextProvider>().InvocationContext.ParseResult);
		services.TryAddScoped<InvocationContextProvider>();
	}

	public static Task<int> Run(string[] args, Action<IServiceCollection>? configureServices = null,
			Action<CommandLineBuilder>? builderAction = null)
	{
		var builder = Host.CreateApplicationBuilder();
		builder.Services.AddLogging(l =>
		{
			l.AddFilter("Microsoft", LogLevel.Warning);
		});

		builder.Services.AddFluentCommandLineServices();
		configureServices?.Invoke(builder.Services);
		var app = builder.Build();
		return app.RunFluentCommandLine(args, builderAction);
	}

	public static Task<int> RunFluentCommandLine(this IHost app, string[] args, Action<CommandLineBuilder>? builderAction = null)
	{
		var rootCommand = CreateRootCommand(app.Services);
		if (builderAction != null)
		{
			var commandLineBuilder = new CommandLineBuilder(rootCommand);
			builderAction?.Invoke(commandLineBuilder);
			var parser = commandLineBuilder.Build();
			return parser.InvokeAsync(args);
		}
		return rootCommand.InvokeAsync(args);
	}

	private static RootCommand CreateRootCommand(IServiceProvider serviceProvider)
	{
		return serviceProvider.GetRequiredService<RootCommand>();
	}
}