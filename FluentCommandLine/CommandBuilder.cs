using Microsoft.Extensions.DependencyInjection;
using System.CommandLine;
using System.CommandLine.Binding;

namespace ToriDogLabs.FluentCommandLine;

internal class CommandBuilder<TCommand> : AbstractCommandBuilder<ICommandConfig, TCommand>, ICommandConfig
	where TCommand : notnull
{
	private readonly Func<TCommand, int>? execute;
	private readonly Func<TCommand, CancellationToken, Task<int>>? executeAsync;

	public CommandBuilder(Func<TCommand, int>? execute = null)
	{
		this.execute = execute;
	}

	public CommandBuilder(Func<TCommand, CancellationToken, Task<int>>? execute)
	{
		executeAsync = execute;
	}

	protected override void ConfigureCommand(Command command, IServiceProvider serviceProvider)
	{
		if (execute != null)
		{
			command.SetHandler((context) =>
			{
				using var scopedServiceProvider = serviceProvider.CreateScope();
				scopedServiceProvider.ServiceProvider.GetRequiredService<InvocationContextProvider>().InvocationContext = context;
				context.ExitCode = execute(scopedServiceProvider.ServiceProvider.GetRequiredService<TCommand>());
			});
		}
		else if (executeAsync != null)
		{
			command.SetHandler(async (context) =>
			{
				using var scopedServiceProvider = serviceProvider.CreateScope();
				scopedServiceProvider.ServiceProvider.GetRequiredService<InvocationContextProvider>().InvocationContext = context;
				context.ExitCode = await executeAsync(scopedServiceProvider.ServiceProvider.GetRequiredService<TCommand>(), context.GetCancellationToken());
			});
		}
	}

	protected override ICommandConfig GetThis()
	{
		return this;
	}

	protected override List<IValueDescriptor> GetValueDescriptors()
	{
		return [];
	}
}