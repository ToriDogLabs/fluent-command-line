using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace FluentCommandLine.SourceGenerator;

public enum Access
{
	Public,
	Private,
	Internal,
	Protected
}

public enum ScopeType
{
	CurlyBraces,
	Parens,
	None
}

public interface ICodeBuilder
{
	public void Build(IndentedTextWriter code);
}

public class ClassBuilder(string name, Access access = Access.Internal, bool isStatic = false) : ICodeBuilder
{
	private readonly List<string> endDirectives = [];
	private readonly List<RegionBuilder> regions = [];
	private readonly List<string> startDirectives = [];
	public Access Access { get; } = access;
	public List<string> BaseList { get; } = [];
	public List<ClassBuilder> Classes { get; } = [];
	public bool IsStatic { get; } = isStatic;
	public List<MethodBuilder> Methods { get; } = [];
	public string Name { get; } = name;
	public List<PropertyBuilder> Properties { get; } = [];

	public ClassBuilder AddBase(string @interface)
	{
		BaseList.Add(@interface);
		return this;
	}

	public ClassBuilder AddClass(ClassBuilder classBuilder)
	{
		Classes.Add(classBuilder);
		return classBuilder;
	}

	public ClassBuilder AddDirective(string startDirective, string? endDirective = null)
	{
		startDirectives.Add(startDirective);
		if (endDirective != null)
		{
			endDirectives.Add(endDirective);
		}
		return this;
	}

	public MethodBuilder AddMethod(MethodBuilder builder)
	{
		Methods.Add(builder);
		return builder;
	}

	public PropertyBuilder AddProperty(PropertyBuilder builder)
	{
		Properties.Add(builder);
		return builder;
	}

	public RegionBuilder AddRegion()
	{
		var region = new RegionBuilder();
		regions.Add(region);
		return region;
	}

	public void Build(IndentedTextWriter code)
	{
		foreach (var startDirective in startDirectives)
		{
			code.WriteLine(startDirective);
		}
		code.WriteLine($"{Access.ToCode()} {(IsStatic ? "static " : "")}class {Name}{GetInterfaces()}");
		code.WriteLine("{");
		code.Indent++;
		foreach (var property in Properties)
		{
			property.Build(code);
		}
		foreach (var method in Methods)
		{
			method.Build(code);
		}
		foreach (var @class in Classes)
		{
			@class.Build(code);
		}
		foreach (var region in regions)
		{
			region.Build(code);
		}
		code.Indent--;
		code.WriteLine("}");
		foreach (var endDirective in endDirectives)
		{
			code.WriteLine(endDirective);
		}
	}

	private string GetInterfaces()
	{
		if (BaseList.Any())
		{
			return $" : {string.Join(", ", BaseList)}";
		}
		return "";
	}
}

public abstract class CodeBuilder : ICodeBuilder
{
	private readonly StringBuilder partialStatement = new();
	public List<ICodeBuilder> Children { get; } = [];

	public CodeBuilder AddIf(string conditional)
	{
		var scope = new ScopeBuilder($"if({conditional})");
		Children.Add(scope);
		return scope;
	}

	public CodeBuilder AddPartialStatement(string statement)
	{
		partialStatement.Append(statement);
		return this;
	}

	public CodeBuilder AddRegion()
	{
		var region = new RegionBuilder();
		Children.Add(region);
		return region;
	}

	public CodeBuilder AddScope(string? initial = null, string? suffix = null, ScopeType type = ScopeType.CurlyBraces)
	{
		var scope = new ScopeBuilder(initial, suffix, type);
		Children.Add(scope);
		return scope;
	}

	public CodeBuilder AddStatement(string statement)
	{
		if (partialStatement.Length > 0)
		{
			partialStatement.AppendLine(statement);
			Children.Add(new StatementBuilder(partialStatement.ToString()));
		}
		else
		{
			Children.Add(new StatementBuilder(statement));
		}
		partialStatement.Clear();
		return this;
	}

	public abstract void Build(IndentedTextWriter code);
}

public class MethodBuilder(string name, string returnType = "void", Access access = Access.Private, bool isStatic = false,
	bool @override = false)
	: CodeBuilder
{
	public Access Access { get; } = access;
	public bool IsStatic { get; } = isStatic;
	public string Name { get; } = name;
	public bool Override { get; } = @override;
	public List<string> Parameters { get; } = [];
	public string ReturnType { get; } = returnType;

	public MethodBuilder AddParameter(string parameter)
	{
		Parameters.Add(parameter);
		return this;
	}

	public override void Build(IndentedTextWriter code)
	{
		List<string> parts = [Access.ToCode()];
		if (IsStatic)
		{
			parts.Add("static");
		}
		if (Override)
		{
			parts.Add("override");
		}
		parts.AddRange([ReturnType, Name]);
		code.WriteLine($"{string.Join(" ", parts)}({string.Join(", ", Parameters)})");
		code.WriteLine("{");
		code.Indent++;
		foreach (var child in Children)
		{
			child.Build(code);
		}
		code.Indent--;
		code.WriteLine("}");
	}
}

public class NamespaceBuilder(string name)
{
	public List<ClassBuilder> Classes { get; } = [];
	public string Name { get; } = name;

	public ClassBuilder AddClass(string name, Access access = Access.Internal, bool isStatic = false)
	{
		var classBuilder = new ClassBuilder(name, access, isStatic);
		Classes.Add(classBuilder);
		return classBuilder;
	}

	public void Build(IndentedTextWriter code, bool indentContent)
	{
		if (indentContent)
		{
			code.WriteLine($"namespace {Name}");
			code.WriteLine("{");
			code.Indent++;
		}
		else
		{
			code.WriteLine($"namespace {Name};");
			code.WriteLine();
		}

		foreach (var @class in Classes)
		{
			@class.Build(code);
		}

		if (indentContent)
		{
			code.Indent--;
			code.WriteLine("}");
		}
	}
}

public class PropertyBuilder : ICodeBuilder
{
	public PropertyBuilder(string type, string name)
	{
		Type = type;
		Name = name;
	}

	public Access Access { get; } = Access.Public;
	public List<string> Attributes { get; } = [];
	public string Name { get; }
	public string Type { get; }

	public void Build(IndentedTextWriter code)
	{
		foreach (var attribute in Attributes)
		{
			code.WriteLine($"[{attribute}]");
		}
		code.WriteLine($"{Access.ToCode()} {Type} {Name} {{ get; set; }} ");
	}
}

public class RegionBuilder() : CodeBuilder, ICodeBuilder
{
	private readonly List<string> endDirectives = [];
	private readonly List<string> startDirectives = [];
	public List<ClassBuilder> Classes { get; } = [];

	public ClassBuilder AddClass(ClassBuilder classBuilder)
	{
		Classes.Add(classBuilder);
		Children.Add(classBuilder);
		return classBuilder;
	}

	public RegionBuilder AddDirective(string startDirective, string? endDirective = null)
	{
		startDirectives.Add(startDirective);
		if (endDirective != null)
		{
			endDirectives.Add(endDirective);
		}
		return this;
	}

	public override void Build(IndentedTextWriter code)
	{
		foreach (var directive in startDirectives)
		{
			code.WriteLine(directive);
		}
		foreach (var child in Children)
		{
			child.Build(code);
		}
		foreach (var directive in endDirectives)
		{
			code.WriteLine(directive);
		}
	}
}

public class ScopeBuilder(string? initial = null, string? suffix = null, ScopeType type = ScopeType.CurlyBraces) : CodeBuilder, ICodeBuilder
{
	private readonly string? initial = initial;

	public override void Build(IndentedTextWriter code)
	{
		if (initial != null)
		{
			code.WriteLine(initial);
		}
		if (type != ScopeType.None)
		{
			code.WriteLine(type == ScopeType.CurlyBraces ? "{" : "(");
		}
		code.Indent++;
		foreach (var child in Children)
		{
			child.Build(code);
		}
		code.Indent--;
		if (type != ScopeType.None)
		{
			code.Write($"{(type == ScopeType.CurlyBraces ? "}" : ")")}");
		}
		code.WriteLine($"{suffix ?? string.Empty}");
	}
}

public class SourceBuilder()
{
	public List<string> Directives { get; } = [];
	public List<NamespaceBuilder> Namespaces { get; } = [];
	public List<string> Usings { get; } = [];

	public SourceBuilder AddDirective(string directive)
	{
		Directives.Add(directive);
		return this;
	}

	public NamespaceBuilder AddNamespace(string @namespace)
	{
		var builder = new NamespaceBuilder(@namespace);
		Namespaces.Add(builder);
		return builder;
	}

	public SourceBuilder AddUsing(string @using)
	{
		Usings.Add(@using);
		return this;
	}

	public string Build()
	{
		var code = new IndentedTextWriter(new StringWriter());
		foreach (var directive in Directives)
		{
			code.WriteLine(directive);
		}
		foreach (var @using in Usings)
		{
			code.WriteLine($"using {@using};");
		}
		foreach (var @namespace in Namespaces)
		{
			code.WriteLine();
			@namespace.Build(code, Namespaces.Count > 1);
		}
		return code.InnerWriter.ToString();
	}
}

public class StatementBuilder(string statement) : ICodeBuilder
{
	private readonly string statement = statement;

	public void Build(IndentedTextWriter code)
	{
		code.WriteLine(statement);
	}
}

public static class Extensions
{
	public static string ToCode(this Access access)
	{
		return access switch
		{
			Access.Internal => "internal",
			Access.Public => "public",
			Access.Private => "private",
			Access.Protected => "protected",
			_ => throw new NotImplementedException()
		};
	}
}