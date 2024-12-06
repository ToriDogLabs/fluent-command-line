using ToriDogLabs.FluentCommandLine.Markers;

namespace ToriDogLabs.FluentCommandLine;

public interface ICommand : IConfigurableCommand
{
	int Execute()
	{
		throw new NotImplementedException();
	}
}

public interface ICommand<TSettings> : IConfigurableCommand<TSettings>
{
	int Execute(TSettings settings)
	{
		throw new NotImplementedException();
	}
}

public interface ICommandAsync : IConfigurableCommand
{
	Task<int> Execute(CancellationToken cancellationToken)
	{
		throw new NotImplementedException();
	}
}

public interface ICommandAsync<TSettings> : IConfigurableCommand<TSettings>
{
	Task<int> Execute(TSettings settings, CancellationToken cancellationToken)
	{
		throw new NotImplementedException();
	}
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