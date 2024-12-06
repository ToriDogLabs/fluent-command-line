using Microsoft.Extensions.DependencyInjection;
using System.CommandLine;
using System.CommandLine.Binding;
using System.CommandLine.Parsing;
using System.Linq.Expressions;

namespace ToriDogLabs.FluentCommandLine;

internal class CommandBuilderWithSettings<TCommand, TSettings> :
	AbstractCommandBuilder<ICommandConfig<TSettings>, TCommand>,
	ICommandConfig<TSettings>
	where TCommand : notnull

{
	private readonly Func<TCommand, TSettings, int>? execute;
	private readonly Func<TCommand, TSettings, CancellationToken, Task<int>>? executeAsync;
	private readonly List<IValueDescriptor> valueDescriptors = [];

	public CommandBuilderWithSettings(Func<TCommand, TSettings, int>? execute = null)
	{
		this.execute = execute;
	}

	public CommandBuilderWithSettings(Func<TCommand, TSettings, CancellationToken, Task<int>>? execute = null)
	{
		executeAsync = execute;
	}

	public ICommandConfig<TSettings> Argument<TProperty>(Expression<Func<TSettings, TProperty>> propertyExpression,
		string name, string? description, bool hidden,
		ParseArgument<TProperty>? parse = null, Func<TProperty>? getDefaultValue = null, Action<Argument<TProperty>>? customize = null)
	{
		var arg = parse == null ? new Argument<TProperty>(name) : new Argument<TProperty>(name, parse);
		arg.Description = description;
		arg.IsHidden = hidden;
		if (getDefaultValue != null)
		{
			arg.SetDefaultValue(getDefaultValue);
		}
		customize?.Invoke(arg);
		valueDescriptors.Add(arg);
		return this;
	}

	public ICommandConfig<TSettings> Option<TProperty>(Expression<Func<TSettings, TProperty>> propertyExpression,
			string name, string? description, bool required, bool hidden, Func<TProperty>? getDefaultValue,
			ParseArgument<TProperty>? parse = null, Action<Option<TProperty>>? customize = null)
	{
		return Option(propertyExpression, [name], description, required, hidden, getDefaultValue, parse, customize);
	}

	public ICommandConfig<TSettings> Option<TProperty>(Expression<Func<TSettings, TProperty>> propertyExpression,
			string[] aliases, string? description, bool required, bool hidden, Func<TProperty>? getDefaultValue,
			ParseArgument<TProperty>? parse = null, Action<Option<TProperty>>? customize = null)
	{
		var option = parse == null ? new Option<TProperty>(aliases) : new Option<TProperty>(aliases, parse);
		option.Description = description;
		option.IsRequired = required;
		option.IsHidden = hidden;
		if (getDefaultValue != null)
		{
			option.SetDefaultValueFactory(() => getDefaultValue());
		}
		customize?.Invoke(option);
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