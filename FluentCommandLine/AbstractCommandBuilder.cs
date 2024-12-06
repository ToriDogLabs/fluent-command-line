using Microsoft.Extensions.DependencyInjection;
using System.CommandLine;
using System.CommandLine.Binding;
using ToriDogLabs.FluentCommandLine.Markers;

namespace ToriDogLabs.FluentCommandLine;

internal interface ICommandBuilder
{
	Command Build(IServiceProvider serviceProvider);

	RootCommand BuildRootCommand(IServiceProvider serviceProvider);
}

internal abstract class AbstractCommandBuilder<TCommandBuilder, TCommand> : ICommandBuilder
{
	protected readonly List<Type> subCommands = [];
	protected string? description;
	protected string? name;

	public TCommandBuilder AddCommand<TSubCommand>() where TSubCommand : IBaseCommand
	{
		subCommands.Add(typeof(TSubCommand));
		return GetThis();
	}

	public Command Build(IServiceProvider serviceProvider)
	{
		var command = new Command(name ?? GetTypeAsName(), description);
		Configure(serviceProvider, command);
		return command;
	}

	public RootCommand BuildRootCommand(IServiceProvider serviceProvider)
	{
		var rootCommand = new RootCommand(description ?? string.Empty);
		;
		Configure(serviceProvider, rootCommand);
		return rootCommand;
	}

	public TCommandBuilder Description(string description)
	{
		this.description = description;
		return GetThis();
	}

	public TCommandBuilder Name(string name)
	{
		this.name = name;
		return GetThis();
	}

	protected abstract void ConfigureCommand(Command command, IServiceProvider serviceProvider);

	protected abstract TCommandBuilder GetThis();

	protected abstract List<IValueDescriptor> GetValueDescriptors();

	private void Configure(IServiceProvider serviceProvider, Command command)
	{
		ConfigureCommand(command, serviceProvider);

		foreach (var subCommandType in subCommands)
		{
			var subCommand = serviceProvider.GetKeyedService<Command>(subCommandType);
			if (subCommand != null)
			{
				command.Add(subCommand);
			}
		}
	}

	private string GetTypeAsName()
	{
		var name = typeof(TCommand).Name;
		return $"{char.ToLower(name[0])}{name[1..]}";
	}
}