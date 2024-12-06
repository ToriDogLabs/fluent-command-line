[![NuGet Version](https://img.shields.io/nuget/v/ToriDogLabs.FluentCommandLine)](https://www.nuget.org/packages/ToriDogLabs.FluentCommandLine/)

# FluentCommandLine

Simplify using System.CommandLine setup using a fluent interface and simple interfaces. This library uses a source generator instead of reflection to simplify the use of System.CommandLine. By not using reflection your app can still be [trim-friendly](https://learn.microsoft.com/en-us/dotnet/core/deploying/trimming/trim-self-contained) allowing a fast, lightweight, AOT-capable CLI app.

## Commands

First you must start with a root command that imlements IRootCommand or IRootCommandAsync. In the configure method you can set up options, arguments, descriptions, etc.

```cs
public class RootCommand : IRootCommand
{
	public static void Configure(ICommandConfig config)
	{
		config
			.Description("Sample app for FluentCommand")
			.AddCommand<ReadCommand>();
	}

	public int Execute()
	{
		return 0;
	}
}
```

Sub commands just implement ICommand or ICommandAsync

```cs
public record ReadCommandArgs(FileInfo FileInfo, int Delay, ConsoleColor ForegroundColor, bool LightMode);

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
```

## Running

You can run your console app with this `Program.cs`

```cs
using FluentCommandLine;

return await FluentCommandHost.Run(args);
```

If you need to configure some services for dependency injection you can use this overload

```cs
using FluentCommandLine;

return await FluentCommandHost.Run(args, services =>
{
	services.AddTransient<MyService>();
});
```

If you need more control over the app you can use something like this

```cs
using FluentCommandLine;

var builder = Microsoft.Extensions.Hosting.Host.CreateApplicationBuilder();
builder.Services.AddFluentCommandLineServices();
builder.Services.AddCommands();
var app = builder.Build();
return await app.RunFluentCommandLine(args);
```
