using Microsoft.Extensions.DependencyInjection;
using System.CommandLine;
using System.CommandLine.Binding;
using System.Linq.Expressions;

namespace FluentCommandLine;

internal class CommandBuilderWithSettings<TCommand, TSettings> :
	AbstractCommandBuilder<ICommandConfig<TSettings>, TCommand>,
	ICommandConfig<TSettings>
	where TCommand : notnull

{
	private readonly Func<TCommand, TSettings, int>? execute;
	private readonly Func<TCommand, TSettings, CancellationToken, Task<int>>? executeAsync;
	private readonly List<IValueDescriptor> valueDescriptors = [];

	public CommandBuilderWithSettings(Func<TCommand, TSettings, int> execute)
	{
		this.execute = execute;
	}

	public CommandBuilderWithSettings(Func<TCommand, TSettings, CancellationToken, Task<int>> executeAsync)
	{
		this.executeAsync = executeAsync;
	}

	public ICommandConfig<TSettings> Argument<TProperty>(Expression<Func<TSettings, TProperty>> propertyExpression,
		Argument<TProperty> argument)
	{
		valueDescriptors.Add(argument);
		return this;
	}

	public ICommandConfig<TSettings> Argument<TProperty>(Expression<Func<TSettings, TProperty>> propertyExpression,
		string name, string? description, bool hidden)
	{
		var arg = new Argument<TProperty>(name, description) { IsHidden = hidden };
		valueDescriptors.Add(arg);
		return this;
	}

	public ICommandConfig<TSettings> Option<TProperty>(Expression<Func<TSettings, TProperty>> propertyExpression,
			Option<TProperty> option)
	{
		valueDescriptors.Add(option);
		return this;
	}

	public ICommandConfig<TSettings> Option<TProperty>(Expression<Func<TSettings, TProperty>> propertyExpression,
			string name, string? description, bool required, bool hidden, Func<TProperty>? getDefaultValue)
	{
		return Option(propertyExpression, [name], description, required, hidden, getDefaultValue);
	}

	public ICommandConfig<TSettings> Option<TProperty>(Expression<Func<TSettings, TProperty>> propertyExpression,
			string[] aliases, string? description, bool required, bool hidden, Func<TProperty>? getDefaultValue)
	{
		var option = new Option<TProperty>(aliases, description)
		{
			IsRequired = required,
			IsHidden = hidden,
		};
		if (getDefaultValue != null)
		{
			option.SetDefaultValueFactory(() => getDefaultValue());
		}
		valueDescriptors.Add(option);
		return this;
	}

	protected override void ConfigureCommand(Command command, IServiceProvider serviceProvider)
	{
		foreach (var descritpor in valueDescriptors)
		{
			if (descritpor is Argument argument)
			{
				command.AddArgument(argument);
			}
			else if (descritpor is Option option)
			{
				command.AddOption(option);
			}
		}
		var binder = serviceProvider.GetRequiredService<AbstractSettingsBinder<TSettings>>();
		binder.ValueDescriptors = valueDescriptors;
		if (execute != null)
		{
			command.SetHandler((context) =>
			{
				using var scopedServiceProvider = serviceProvider.CreateScope();
				scopedServiceProvider.ServiceProvider.GetRequiredService<InvocationContextProvider>().InvocationContext = context;
				context.ExitCode = execute(scopedServiceProvider.ServiceProvider.GetRequiredService<TCommand>(), binder.GetSettings(context.BindingContext));
			});
		}
		else if (executeAsync != null)
		{
			command.SetHandler(async context =>
			{
				using var scopedServiceProvider = serviceProvider.CreateScope();
				scopedServiceProvider.ServiceProvider.GetRequiredService<InvocationContextProvider>().InvocationContext = context;
				context.ExitCode = await executeAsync(scopedServiceProvider.ServiceProvider.GetRequiredService<TCommand>(), binder.GetSettings(context.BindingContext),
					context.GetCancellationToken());
			});
		}
	}

	protected override ICommandConfig<TSettings> GetThis()
	{
		return this;
	}

	protected override List<IValueDescriptor> GetValueDescriptors()
	{
		return valueDescriptors;
	}
}