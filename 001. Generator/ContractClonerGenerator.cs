using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Collections.Generic;
using System.Linq;
using System.Text;

[Generator]
public class ContractClonerGenerator : ISourceGenerator
{
    public void Initialize(GeneratorInitializationContext context)
    {
        // Optional: debugging
        // System.Diagnostics.Debugger.Launch();

        context.RegisterForSyntaxNotifications(() => new SyntaxReceiver());
    }

    public void Execute(GeneratorExecutionContext context)
    {
        if (context.SyntaxReceiver is not SyntaxReceiver receiver) return;

        foreach (var candidate in receiver.Candidates)
        {
            var model = context.Compilation.GetSemanticModel(candidate.SyntaxTree);
            var symbol = model.GetDeclaredSymbol(candidate) as INamedTypeSymbol;

            if (symbol == null) continue;

            var attr = symbol.GetAttributes().FirstOrDefault(a =>
                a.AttributeClass?.Name == "ContractFromAttribute" ||
                a.AttributeClass?.ToDisplayString() == "ContractFromAttribute");

            if (attr == null) continue;

            if (attr.ConstructorArguments[0].Value is not INamedTypeSymbol contractType) continue;

            var sb = new StringBuilder();
            var namespaceName = symbol.ContainingNamespace.ToDisplayString();
            var className = symbol.Name;

            // Create class declaration
            sb.AppendLine("using MemoryPack;");
            sb.AppendLine($"namespace {namespaceName};");
            sb.AppendLine();
            sb.AppendLine("[MemoryPackable]");
            sb.AppendLine($"public partial class {className}");
            sb.AppendLine("{");

            int index = 0;

            foreach (var member in contractType.GetMembers().OfType<IPropertySymbol>())
            {
                var typeName = member.Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)
                    .Replace("global::", "");

                sb.AppendLine($"    [MemoryPackInclude({index++})]");
                sb.AppendLine($"    public {typeName} {member.Name} {{ get; set; }}");
            }

            sb.AppendLine("}");

            // Add to compilation
            context.AddSource($"{className}_Generated.cs", sb.ToString());
        }
    }

    class SyntaxReceiver : ISyntaxReceiver
    {
        public List<ClassDeclarationSyntax> Candidates { get; } = new();

        public void OnVisitSyntaxNode(SyntaxNode syntaxNode)
        {
            if (syntaxNode is ClassDeclarationSyntax cds &&
                cds.AttributeLists.Count > 0)
            {
                Candidates.Add(cds);
            }
        }
    }
}
