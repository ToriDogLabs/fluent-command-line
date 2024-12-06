using System.CommandLine;
using System.CommandLine.Parsing;
using System.Linq.Expressions;
using ToriDogLabs.FluentCommandLine.Markers;

namespace ToriDogLabs.FluentCommandLine;

public interface ICommandConfig
{
	ICommandConfig AddCommand<TSubCommand>() where TSubCommand : IBaseCommand;

	ICommandConfig Description(string description);

	ICommandConfig Name(string name);
}

public interface ICommandConfig<TSettings>
{
	ICommandConfig<TSettings> AddCommand<TSubCommand>() where TSubCommand : IBaseCommand;

	ICommandConfig<TSettings> Argument<TProperty>(Expression<Func<TSettings, TProperty>> propertyExpression,
			string name,
			string? description = null,
			bool hidden = false,
			ParseArgument<TProperty>? parse = null,
			Func<TProperty>? getDefaultValue = null,
			Action<Argument<TProperty>>? customize = null);

	ICommandConfig<TSettings> Description(string description);

	ICommandConfig<TSettings> Name(string name);

	ICommandConfig<TSettings> Option<TProperty>(Expression<Func<TSettings, TProperty>> propertyExpression,
					string name,
					string? description = null,
					bool required = false,
					bool hidden = false,
					Func<TProperty>? getDefaultValue = null,
					ParseArgument<TProperty>? parse = null,
					Action<Option<TProperty>>? customize = null);

	ICommandConfig<TSettings> Option<TProperty>(Expression<Func<TSettings, TProperty>> propertyExpression,
					string[] aliases,
					string? description = null,
					bool required = false,
					bool hidden = false,
					Func<TProperty>? getDefaultValue = null,
					ParseArgument<TProperty>? parse = null,
					Action<Option<TProperty>>? customize = null);
}