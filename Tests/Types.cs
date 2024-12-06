namespace Tests;
extern alias Lib;
extern alias SourceGen;

using FluentAssertions;

public class Types
{
	[Fact]
	public void ConfigureIsTheSame()
	{
		nameof(SourceGen::ToriDogLabs.FluentCommandLine.IConfigurableCommand.Configure).Should()
			.Be(nameof(Lib.ToriDogLabs.FluentCommandLine.IConfigurableCommand.Configure));
	}

	[Theory]
	[InlineData(typeof(SourceGen::ToriDogLabs.FluentCommandLine.ICommand), typeof(Lib::ToriDogLabs.FluentCommandLine.ICommand))]
	[InlineData(typeof(SourceGen::ToriDogLabs.FluentCommandLine.IRootCommand), typeof(Lib::ToriDogLabs.FluentCommandLine.IRootCommand))]
	[InlineData(typeof(SourceGen::ToriDogLabs.FluentCommandLine.ICommandAsync), typeof(Lib::ToriDogLabs.FluentCommandLine.ICommandAsync))]
	[InlineData(typeof(SourceGen::ToriDogLabs.FluentCommandLine.IRootCommandAsync), typeof(Lib::ToriDogLabs.FluentCommandLine.IRootCommandAsync))]
	[InlineData(typeof(SourceGen::ToriDogLabs.FluentCommandLine.ICommandGroup), typeof(Lib::ToriDogLabs.FluentCommandLine.ICommandGroup))]
	[InlineData(typeof(SourceGen::ToriDogLabs.FluentCommandLine.IRootCommandGroup), typeof(Lib::ToriDogLabs.FluentCommandLine.IRootCommandGroup))]
	[InlineData(typeof(SourceGen::ToriDogLabs.FluentCommandLine.Markers.IBaseCommand), typeof(Lib::ToriDogLabs.FluentCommandLine.Markers.IBaseCommand))]
	[InlineData(typeof(SourceGen::ToriDogLabs.FluentCommandLine.Markers.IBaseRootCommand), typeof(Lib::ToriDogLabs.FluentCommandLine.Markers.IBaseRootCommand))]
	public void TypesAreInSync(Type a, Type b)
	{
		a.FullName.Should().Be(b.FullName);
	}
}