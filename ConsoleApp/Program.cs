// See https://aka.ms/new-console-template for more information
using System.CommandLine;
using ToriDogLabs.FluentCommandLine;

return await FluentCommandHost.Run(args);

//var builder = Microsoft.Extensions.Hosting.Host.CreateApplicationBuilder();
//builder.Services.AddFluentCommandLineServices();
//builder.Services.AddCommands();
//var app = builder.Build();
//return await app.RunFluentCommandLine(args);

public class ArgsWithUnusedProp
{
	public string? Value { get; set; }
}

public class ReadCommand(IConsole console) : ICommandAsync<ReadCommandArgs>
{
	public static void Configure(ICommandConfig<ReadCommandArgs> config)
	{
		config
			.Name("read")
			.Description("Read and display the file.")
			.Option(a => a.FileInfo,
				["--file", "-f"],
				"The file to read and display on the console.",
				required: true)
			.Option(a => a.Delay,
				["--delay", "-d"],
				"Delay between lines, specified as milliseconds per character in a line.",
				getDefaultValue: () => 42)
			.Option(a => a.ForegroundColor,
				["--fgcolor", "-c"],
				"Foreground color of text displayed on the console.",
				getDefaultValue: () => ConsoleColor.White)
			.Option(a => a.LightMode,
				["--light-mode", "-l"],
				"Background color of text displayed on the console: default is black, light mode is white.");
	}

	public async Task<int> Execute(ReadCommandArgs settings, CancellationToken cancellationToken)
	{
		Console.BackgroundColor = settings.LightMode ? ConsoleColor.White : ConsoleColor.Black;
		Console.ForegroundColor = settings.ForegroundColor;
		var lines = File.ReadLines(settings.FileInfo.FullName).ToList();
		foreach (var line in lines)
		{
			console.WriteLine(line);
			await Task.Delay(settings.Delay * line.Length, cancellationToken);
		};
		return 0;
	}
}

public class RootCommand : IRootCommandGroup
{
	public static void Configure(ICommandConfig config)
	{
		config
			.Description("Sample app for FluentCommand")
			.AddCommand<ReadCommand>()
			.AddCommand<TestCommand>();
	}
}

public record ReadCommandArgs(FileInfo FileInfo, int Delay, ConsoleColor ForegroundColor, bool LightMode);

public class TestCommand : ICommand<ArgsWithUnusedProp>
{
	public static void Configure(ICommandConfig<ArgsWithUnusedProp> config)
	{
	}

	public int Execute(ArgsWithUnusedProp settings)
	{
		return 0;
	}
}