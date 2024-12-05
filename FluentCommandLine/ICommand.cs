using FluentCommandLine.Markers;

namespace FluentCommandLine;

public interface ICommand : IConfigurableCommand
{
	int Execute();
}

public interface ICommand<TSettings> : IConfigurableCommand<TSettings>
{
	int Execute(TSettings settings);
}

public interface ICommandAsync : IConfigurableCommand
{
	Task<int> Execute(CancellationToken cancellationToken);
}

public interface ICommandAsync<TSettings> : IConfigurableCommand<TSettings>
{
	Task<int> Execute(TSettings settings, CancellationToken cancellationToken);
}

public interface IConfigurableCommand : IBaseCommand
{
	abstract static void Configure(ICommandConfig config);
}

public interface IConfigurableCommand<TSettings> : IBaseCommand
{
	abstract static void Configure(ICommandConfig<TSettings> config);
}

public interface IRootCommand : ICommand, IBaseRootCommand
{
}

public interface IRootCommand<TSettings> : ICommand<TSettings>, IBaseRootCommand
{
}

public interface IRootCommandAsync : ICommandAsync, IBaseRootCommand
{
}

public interface IRootCommandAsync<TSettings> : ICommandAsync<TSettings>, IBaseRootCommand
{
}