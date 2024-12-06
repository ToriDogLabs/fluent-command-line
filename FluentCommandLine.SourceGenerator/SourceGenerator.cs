using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading;

namespace ToriDogLabs.FluentCommandLine.SourceGenerator;

[Generator(LanguageNames.CSharp)]
public class FluentCommandLineGenerator : IIncrementalGenerator
{
	public void Initialize(IncrementalGeneratorInitializationContext context)
	{
		var classDeclarations = context.SyntaxProvider.CreateSyntaxProvider(IsSyntaxTargetForGeneration, GetSemanticTargetForGeneration)
			.Where(static c => c is not null);

		var compilationAndClasses = context.CompilationProvider.Combine(classDeclarations.Collect());
		context.RegisterSourceOutput(compilationAndClasses, static (spc, source) => Execute(source.Left, source.Right!, spc));
	}

	private static void Execute(Compilation compilation, ImmutableArray<ClassDeclarationSyntax> classes, SourceProductionContext context)
	{
		if (classes.IsDefaultOrEmpty)
		{
			return;
		}
		var emitter = new SourceEmitter(compilation);
		var (code, bindersCode) = emitter.Emit(classes.Distinct());

		context.AddSource($"{compilation.AssemblyName}.FluentCommands.g.cs", code);
		context.AddSource($"{compilation.AssemblyName}.FluentCommands.Binders.g.cs", bindersCode);
	}

	private static ClassDeclarationSyntax? GetSemanticTargetForGeneration(GeneratorSyntaxContext context, CancellationToken _token)
	{
		if (context.Node is ClassDeclarationSyntax classDeclarationSyntax)
		{
			if (context.SemanticModel.ImplementsInterface(classDeclarationSyntax, typeof(ToriDogLabs.FluentCommandLine.Markers.IBaseCommand).FullName))
			{
				return classDeclarationSyntax;
			}
		}
		return null;
	}

	private static bool IsSyntaxTargetForGeneration(SyntaxNode node, CancellationToken _token)
	{
		return node is ClassDeclarationSyntax @class && (@class.BaseList?.Types.Count ?? 0) > 0;
	}
}

internal static class Helpers
{
	public static IEnumerable<ISymbol> GetAllMembers(this ITypeSymbol? typeSymbol)
	{
		var members = new HashSet<ISymbol>(SymbolEqualityComparer.Default);

		while (typeSymbol != null)
		{
			foreach (var member in typeSymbol.GetMembers())
			{
				if (members.Add(member)) // Avoid duplicates
				{
					yield return member;
				}
			}

			typeSymbol = typeSymbol.BaseType; // Traverse base types
		}
	}

	public static string GetFullyQualifiedName(this ClassDeclarationSyntax classDeclaration)
	{
		// Get the class name
		var className = classDeclaration.Identifier.Text;

		// Initialize a StringBuilder for the namespace
		var namespaceBuilder = new StringBuilder();

		// Traverse parent nodes to find the namespace(s)
		var parent = classDeclaration.Parent;
		while (parent != null)
		{
			if (parent is NamespaceDeclarationSyntax namespaceDeclaration)
			{
				// Prepend the namespace name
				namespaceBuilder.Insert(0, namespaceDeclaration.Name.ToString() + ".");
			}
			else if (parent is FileScopedNamespaceDeclarationSyntax fileScopedNamespace)
			{
				// Handle file-scoped namespaces (C# 10+)
				namespaceBuilder.Insert(0, fileScopedNamespace.Name.ToString() + ".");
			}

			parent = parent.Parent;
		}

		// Combine namespace and class name
		return namespaceBuilder + className;
	}

	public static IEnumerable<INamedTypeSymbol> GetInterfaces(this SemanticModel semanticModel,
			ClassDeclarationSyntax classDeclaration)
	{
		if (semanticModel.GetDeclaredSymbol(classDeclaration) is not INamedTypeSymbol classSymbol)
		{
			return [];
		}

		return classSymbol.AllInterfaces;
	}

	public static Dictionary<string, RecordConstructorProperty> GetRecordParameters(this SemanticModel semanticModel,
		SyntaxNode? declaration)
	{
		var results = new Dictionary<string, RecordConstructorProperty>();
		GetRecordParameters(declaration, semanticModel, results);
		return results;
	}

	public static bool ImplementsInterface(this SemanticModel semanticModel, ClassDeclarationSyntax classDeclaration,
				string interfaceFullName)
	{
		var interfaces = semanticModel.GetInterfaces(classDeclaration);
		return interfaces.Any(i => i.ToDisplayString() == interfaceFullName);
	}

	private static void GetRecordParameters(
			SyntaxNode? declaration, SemanticModel semanticModel, Dictionary<string, RecordConstructorProperty> results)
	{
		if (declaration is RecordDeclarationSyntax recordDeclaration)
		{
			foreach (var parameter in recordDeclaration.ParameterList?.Parameters ?? [])
			{
				var name = parameter.Identifier.Text;
				results[name] = new()
				{
					Index = results.Count,
				};
			}
		}
	}
}

internal class SourceEmitter
{
	private Compilation compilation;

	public SourceEmitter(Compilation compilation)
	{
		this.compilation = compilation;
	}

	internal (string, string) Emit(IEnumerable<ClassDeclarationSyntax> classes)
	{
		var bindersSource = new SourceBuilder()
			.AddDirective("#nullable enable")
			.AddUsing("System.CommandLine");
		var binderNamespace = $"ToriDogLabs.FluentCommandLine.Generated.{compilation.AssemblyName ?? string.Empty}";
		var bindersNamespace = bindersSource.AddNamespace(binderNamespace);
		var source = new SourceBuilder()
			.AddDirective("#nullable enable")
			.AddUsing("Microsoft.Extensions.DependencyInjection")
			.AddUsing("ToriDogLabs.FluentCommandLine")
			.AddUsing("ToriDogLabs.FluentCommandLine.Commands")
			.AddUsing("System.CommandLine.Builder");
		var @namespace = source.AddNamespace("ToriDogLabs.FluentCommandLine");
		var extClass = @namespace.AddClass("FluentCommandLineExtensions", Access.Public, isStatic: true);
		var method = extClass.AddMethod(new("AddCommands", access: Access.Public, isStatic: true))
			.AddParameter("this IServiceCollection services");

		var hostClass = @namespace.AddClass("FluentCommandHost", Access.Internal, isStatic: true);
		hostClass.AddMethod(new("Run", "Task<int>", Access.Public, isStatic: true))
			.AddParameter("string[] args")
			.AddParameter("Action<IServiceCollection>? configureServices = null")
			.AddParameter("Action<CommandLineBuilder>? builderAction = null")
			.AddScope("return FluentCommandLineInitializer.Run(args, services => ", ", builderAction);")
			.AddStatement("services.AddCommands();")
			.AddStatement("configureServices?.Invoke(services);");
		foreach (var @class in classes)
		{
			var semanticModel = compilation.GetSemanticModel(@class.SyntaxTree);
			var interfaces = semanticModel.GetInterfaces(@class);
			string? addMethod = null;
			List<string> args = [];
			var typeArgs = "";
			INamedTypeSymbol? settingsNamedTypeSymbol = null;
			if (FindInterface(interfaces, typeof(FluentCommandLine.ICommandAsync).FullName, out var _) ||
				FindInterface(interfaces, typeof(FluentCommandLine.IRootCommandAsync).FullName, out var _))
			{
				addMethod = "AddCommandAsync";
			}
			else if (FindInterface(interfaces, typeof(FluentCommandLine.ICommand).FullName, out var _) ||
				FindInterface(interfaces, typeof(FluentCommandLine.IRootCommand).FullName, out var _))
			{
				addMethod = "AddCommand";
			}
			else if (FindInterface(interfaces, $"{typeof(FluentCommandLine.ICommandAsync).FullName}<TSettings>", out var asyncSettingsInterface))
			{
				addMethod = "AddCommandWithSettingsAsync";
				settingsNamedTypeSymbol = asyncSettingsInterface;
			}
			else if (FindInterface(interfaces, $"{typeof(FluentCommandLine.IRootCommandAsync).FullName}<TSettings>", out var rootAsyncSettingsInterface))
			{
				addMethod = "AddCommandWithSettingsAsync";
				settingsNamedTypeSymbol = rootAsyncSettingsInterface;
			}
			else if (FindInterface(interfaces, $"{typeof(FluentCommandLine.ICommand).FullName}<TSettings>", out var settingsInterface))
			{
				addMethod = "AddCommandWithSettings";
				settingsNamedTypeSymbol = settingsInterface;
			}
			else if (FindInterface(interfaces, $"{typeof(FluentCommandLine.IRootCommand).FullName}<TSettings>", out var rootSettingsInterface))
			{
				addMethod = "AddCommandWithSettings";
				settingsNamedTypeSymbol = rootSettingsInterface;
			}

			if (settingsNamedTypeSymbol != null)
			{
				var settingsType = settingsNamedTypeSymbol!.TypeArguments.First();
				typeArgs = $", {settingsType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)}";
				var (AbstractName, ClassName) = CreateBinder(bindersNamespace, settingsType, semanticModel, @class);
				method.AddStatement($"services.AddTransient<{nameof(FluentCommandLine)}.{AbstractName}, {binderNamespace}.{ClassName}>();");
			}

			if (interfaces.Any(i => i.ToDisplayString() == typeof(FluentCommandLine.Markers.IBaseRootCommand).FullName))
			{
				args.Add("rootCommand: true");
			}

			if (addMethod != null)
			{
				var classSymbol = semanticModel.GetDeclaredSymbol(@class);
				var members = classSymbol.GetAllMembers().ToList();
				var executeMethod = classSymbol.GetAllMembers().OfType<IMethodSymbol>().FirstOrDefault(m => m.Name == "Execute");
				if (executeMethod == null)
				{
					args.Add("addHandler: false");
				}
				method.AddStatement($"services.{addMethod}<{@class.GetFullyQualifiedName()}{typeArgs}>({string.Join(", ", args)});");
			}
		}
		return (source.Build(), bindersSource.Build());
	}

	private (string AbstractName, string ClassName) CreateBinder(NamespaceBuilder code, ITypeSymbol settingsSymbol,
		SemanticModel semanticModel, ClassDeclarationSyntax commandClass)
	{
		var valueProps = FindValueDescriptors(commandClass, semanticModel);
		var settingsTypeFullName = settingsSymbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
		var className = $"{settingsSymbol.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat)}Binder";
		var classBuilder = code.AddClass(className);
		var abstractName = $"AbstractSettingsBinder<{settingsTypeFullName}>";
		classBuilder.AddBase(abstractName);
		var method = classBuilder.AddMethod(new("GetBoundValue", settingsTypeFullName, Access.Protected, @override: true));
		method.AddParameter("System.CommandLine.Binding.BindingContext bindingContext");

		var declaringSyntax = settingsSymbol.DeclaringSyntaxReferences.FirstOrDefault()?.GetSyntax();
		var recordParams = semanticModel.GetRecordParameters(declaringSyntax);
		var properties = settingsSymbol.GetMembers().Where(m => m.Kind == SymbolKind.Property) ?? [];
		List<(IPropertySymbol Property, int Index)> constructorParams = [];
		List<IPropertySymbol> initializerProperties = [];
		foreach (var property in properties.OfType<IPropertySymbol>())
		{
			if (property.IsImplicitlyDeclared)
			{
				continue;
			}
			if (recordParams.TryGetValue(property.Name, out var recordParam))
			{
				constructorParams.Add((property, recordParam.Index!.Value));
			}
			else
			{
				initializerProperties.Add(property);
			}
		}

		string GetResult(IPropertySymbol property)
		{
			var (Id, Kind, Index, Required) = valueProps.Find(vp => vp.Id == property.Name);
			var nullable = property.NullableAnnotation == NullableAnnotation.Annotated;
			var type = $"{property.Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)}";
			var suffix = Required ? "!" : string.Empty;
			return $"bindingContext.ParseResult.GetValueFor{Kind}(({Kind}<{type}{(nullable ? "?" : "")}>)ValueDescriptors[{Index}]){suffix}";
		}

		var constructor = string.Empty;
		if (constructorParams.Any())
		{
			constructor = $"({string.Join(",", constructorParams.OrderBy(p => p.Index).Select(p => GetResult(p.Property)))})";
		}
		if (initializerProperties.Any())
		{
			var scope = method.AddScope($"return new {settingsTypeFullName}{constructor}", ";");
			foreach (var property in initializerProperties)
			{
				scope.AddStatement($"{property.Name} = {GetResult(property)}, ");
			}
		}
		else
		{
			method.AddStatement($"return new {settingsTypeFullName}{constructor};");
		}

		return (abstractName, className);
	}

	private bool FindInterface(IEnumerable<INamedTypeSymbol> interfaces, string interfaceName, out INamedTypeSymbol? @interface)
	{
		foreach (var i in interfaces)
		{
			if (i.OriginalDefinition.ToDisplayString() == interfaceName)
			{
				@interface = i;
				return true;
			}
		}
		@interface = null;
		return false;
	}

	private List<(string Id, string Kind, int Index, bool Required)> FindValueDescriptors(
		ClassDeclarationSyntax @class, SemanticModel semanticModel)
	{
		List<(string Id, string Kind, int Index, bool Required)> props = [];
		var symbol = semanticModel.GetSymbolInfo(@class);
		var commandMethod = @class.Members.Where(m => m.IsKind(SyntaxKind.MethodDeclaration))
			.OfType<MethodDeclarationSyntax>()
			.Where(m => m.Identifier.ToString() == nameof(IConfigurableCommand.Configure) && m.Modifiers.Any(SyntaxKind.StaticKeyword)).FirstOrDefault();
		if (commandMethod != null)
		{
			var firstChildren = commandMethod.DescendantNodes();
			foreach (var argList in firstChildren.Where(n => n.IsKind(SyntaxKind.ArgumentList)).OfType<ArgumentListSyntax>())
			{
				if (argList.Parent is InvocationExpressionSyntax invocation)
				{
					var required = false;
					string? kind = null;
					var argDescendants = argList.DescendantNodes();
					if (invocation.Expression is IdentifierNameSyntax identifier)
					{
						if (identifier.Identifier.Text == "Argument" || identifier.Identifier.Text == "Option")
						{
							kind = identifier.Identifier.Text;
						}
					}
					else if (invocation.Expression is MemberAccessExpressionSyntax memberAccess)
					{
						if (memberAccess.Name.Identifier.Text == "Argument" || memberAccess.Name.Identifier.Text == "Option")
						{
							kind = memberAccess.Name.Identifier.Text;
						}
					}
					if (kind != null)
					{
						if (kind == "Option")
						{
							var requiredParam = argList.Arguments.FirstOrDefault(arg => arg.NameColon?.Name.Identifier.Text == "required");
							requiredParam ??= argList.Arguments.Where(arg => arg.NameColon == null).Skip(2).FirstOrDefault();
							required = requiredParam?.Expression.IsKind(SyntaxKind.TrueLiteralExpression) ?? false;
						}
						else
						{
							required = true;
						}
						var propName = GetArgListPropName(argList);
						if (propName != null)
						{
							props.Add((propName, kind, props.Count, required));
						}
					}
				}
			}
		}
		return props;
	}

	private string? GetArgListPropName(ArgumentListSyntax argList)
	{
		var identifiers = argList.DescendantNodes().Where(n => n.IsKind(SyntaxKind.IdentifierName)).OfType<IdentifierNameSyntax>();
		if (identifiers.Count() >= 2)
		{
			return identifiers.Skip(1).First().Identifier.Text;
		}
		return null;
	}
}

internal class RecordConstructorProperty
{
	public int? Index { get; set; }
}