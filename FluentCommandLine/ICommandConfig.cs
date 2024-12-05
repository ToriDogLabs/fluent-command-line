using FluentCommandLine.Markers;
using System.CommandLine;
using System.Linq.Expressions;

namespace FluentCommandLine;

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
			Argument<TProperty> argument);

	ICommandConfig<TSettings> Argument<TProperty>(Expression<Func<TSettings, TProperty>> propertyExpression,
			string name, string? description = null);

	ICommandConfig<TSettings> Description(string description);

	ICommandConfig<TSettings> Name(string name);

	ICommandConfig<TSettings> Option<TProperty>(Expression<Func<TSettings, TProperty>> propertyExpression,
					Option<TProperty> option);

	ICommandConfig<TSettings> Option<TProperty>(Expression<Func<TSettings, TProperty>> propertyExpression,
					string name,
					string? description = null,
					bool required = false,
					bool hidden = false,
					Func<TProperty>? getDefaultValue = null);

	ICommandConfig<TSettings> Option<TProperty>(Expression<Func<TSettings, TProperty>> propertyExpression,
					string[] aliases,
					string? description = null,
					bool required = false,
					bool hidden = false,
					Func<TProperty>? getDefaultValue = null);
}