using Microsoft.Extensions.DependencyInjection;

namespace ToriDogLabs.FluentCommandLine.Commands;

public static class FluentCommandLineServices
{
	public static void AddCommand<TCommand>(this IServiceCollection services, bool rootCommand = false)
		where TCommand : class, ICommand
	{
		var builder = new CommandBuilder<TCommand>(command => command.Execute());
		TCommand.Configure(builder);
		AddCommon<TCommand>(services, rootCommand, builder);
	}

	public static void AddCommandAsync<TCommand>(this IServiceCollection services, bool rootCommand = false)
		where TCommand : class, ICommandAsync
	{
		var builder = new CommandBuilder<TCommand>((command, token) => command.Execute(token));
		TCommand.Configure(builder);
		AddCommon<TCommand>(services, rootCommand, builder);
	}

	public static void AddCommandWithSettings<TCommand, TSettings>(this IServiceCollection services, bool rootCommand = false)
		where TCommand : class, ICommand<TSettings>
	{
		var builder = new CommandBuilderWithSettings<TCommand, TSettings>((command, settings) => command.Execute(settings));
		TCommand.Configure(builder);
		AddCommon<TCommand>(services, rootCommand, builder);
	}

	public static void AddCommandWithSettingsAsync<TCommand, TSettings>(this IServiceCollection services, bool rootCommand = false)
		where TCommand : class, ICommandAsync<TSettings>
	{
		var builder = new CommandBuilderWithSettings<TCommand, TSettings>((command, settings, token) => command.Execute(settings, token));
		TCommand.Configure(builder);
		AddCommon<TCommand>(services, rootCommand, builder);
	}

	private static void AddCommon<TCommand>(IServiceCollection services, bool rootCommand, ICommandBuilder builder) where TCommand : class
	{
		services.AddTransient<TCommand>();
		if (rootCommand)
		{
			services.AddTransient(builder.BuildRootCommand);
		}
		else
		{
			services.AddKeyedTransient(typeof(TCommand), (sp, key) =>
			{
				return builder.Build(sp);
			});
		}
	}
}