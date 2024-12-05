namespace Tests;
extern alias Lib;
extern alias SourceGen;

using FluentAssertions;

public class Types
{
	[Fact]
	public void ConfigureIsTheSame()
	{
		nameof(SourceGen::FluentCommandLine.IConfigurableCommand.Configure).Should()
			.Be(nameof(Lib.FluentCommandLine.IConfigurableCommand.Configure));
	}

	[Theory]
	[InlineData(typeof(SourceGen::FluentCommandLine.ICommand), typeof(Lib::FluentCommandLine.ICommand))]
	[InlineData(typeof(SourceGen::FluentCommandLine.IRootCommand), typeof(Lib::FluentCommandLine.IRootCommand))]
	[InlineData(typeof(SourceGen::FluentCommandLine.ICommandAsync), typeof(Lib::FluentCommandLine.ICommandAsync))]
	[InlineData(typeof(SourceGen::FluentCommandLine.IRootCommandAsync), typeof(Lib::FluentCommandLine.IRootCommandAsync))]
	[InlineData(typeof(SourceGen::FluentCommandLine.Markers.IBaseCommand), typeof(Lib::FluentCommandLine.Markers.IBaseCommand))]
	[InlineData(typeof(SourceGen::FluentCommandLine.Markers.IBaseRootCommand), typeof(Lib::FluentCommandLine.Markers.IBaseRootCommand))]
	public void TypesAreInSync(Type a, Type b)
	{
		a.FullName.Should().Be(b.FullName);
	}
}