using System.Threading;
using System.Threading.Tasks;
using ToriDogLabs.FluentCommandLine.Markers;

namespace ToriDogLabs.FluentCommandLine
{
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
		void Configure(ICommandConfig command);
	}

	public interface IConfigurableCommand<TSettings> : IBaseCommand
	{
		void Configure(ICommandConfig<TSettings> command);
	}

	public interface IRootCommand : IRootConfigurableCommand
	{
		int Execute();
	}

	public interface IRootCommand<TSettings> : IRootConfigurableCommand<TSettings>
	{
		int Execute(TSettings settings);
	}

	public interface IRootCommandAsync : IRootConfigurableCommand
	{
		Task<int> Execute(CancellationToken cancellationToken);
	}

	public interface IRootCommandAsync<TSettings> : IRootConfigurableCommand<TSettings>
	{
		Task<int> Execute(TSettings settings, CancellationToken cancellationToken);
	}

	public interface IRootConfigurableCommand : IBaseRootCommand
	{
		void Configure(IRootCommandConfig command);
	}

	public interface IRootConfigurableCommand<TSettings> : IBaseRootCommand
	{
		void Configure(IRootCommandConfig<TSettings> command);
	}
}