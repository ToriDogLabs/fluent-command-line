using System.CommandLine.Invocation;

namespace FluentCommandLine;

internal class InvocationContextProvider
{
	public required InvocationContext InvocationContext { get; set; }
}