using System.Linq;
using System.Text;
using System.Threading;
using BepInExUtils.Generator.Extensions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using Scriban;

namespace BepInExUtils.Generator.AccessExtensions;

[Generator]
public class AccessExtensionsGenerator : IIncrementalGenerator
{
    internal const string AccessExtensionsAttributeFullName = "BepInExUtils.Attributes.AccessExtensionsAttribute";
    private const string AccessInstanceAttributeShortName = "AccessInstance";
    private const string AccessFieldAttributeShortName = "AccessField";

    private static Template? _cacheTemplate;
    private static Template? _cacheAccessPropertyTemplate;
    private static Template Template => _cacheTemplate ??= Template.Parse(Resources.AccessExtensionsTemplate);

    private static Template AccessPropertyTemplate =>
        _cacheAccessPropertyTemplate ??= Template.Parse(Resources.AccessPropertyTemplate);

    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var classToGenerate = context.SyntaxProvider
            .ForAttributeWithMetadataName(AccessExtensionsAttributeFullName, Predicate, Transform)
            .Where(i => i is not null);

        context.RegisterSourceOutput(classToGenerate, Execute);
    }

    private static bool Predicate(SyntaxNode syntaxNode, CancellationToken cancellationToken) =>
        syntaxNode is ClassDeclarationSyntax syntax && syntax.Modifiers.Any(SyntaxKind.PartialKeyword) &&
        syntax.Modifiers.Any(SyntaxKind.StaticKeyword);

    private static AccessExtensionsInfo? Transform(GeneratorAttributeSyntaxContext syntaxContext,
        CancellationToken cancellationToken)
    {
        if (syntaxContext.TargetNode is not ClassDeclarationSyntax syntax) return null;
        var namespaceName = syntax.Parent is BaseNamespaceDeclarationSyntax ns ? ns.Name.ToString() : "Global";
        var className = syntax.Identifier.Text;

        // Retrieve all using directives from the original file
        var usingsText = string.Join("\n", syntax.SyntaxTree.GetRoot()
            .DescendantNodes()
            .OfType<UsingDirectiveSyntax>()
            .Select(u => u.ToString())
            .Distinct());

        var instanceType = syntax.AttributeLists.SelectMany(a => a.Attributes).FirstOrDefault(attr =>
            attr.Name.ToString().StartsWith(AccessInstanceAttributeShortName + '<'))?.Name.ToString();

        var accessFieldInfos = syntax.AttributeLists.SelectMany(a => a.Attributes)
            .Where(attr => attr.Name.ToString().StartsWith(AccessFieldAttributeShortName + '<'))
            .Select(attr => new AccessFieldInfo(attr.Name.ToString(),
                attr.ArgumentList?.Arguments.FirstOrDefault()?.ToString().Trim('"') ?? "")).ToList();

        var classInfo = new ClassInfo(namespaceName, className, usingsText, nameof(AccessExtensionsGenerator),
            syntax.Identifier);
        return new AccessExtensionsInfo(classInfo, instanceType, accessFieldInfos);
    }

    private static void Execute(SourceProductionContext context, AccessExtensionsInfo? info)
    {
        if (!info.HasValue) return;
        var (classInfo, instanceType, accessFieldInfos) = info.Value;
        var (namespaceName, className, usings, generatorName, identifier) = classInfo;
        var uniqueHintName = $"{namespaceName}.{className}_{generatorName}.generated.cs";

        if (instanceType == null)
        {
            var diagnostic = Diagnostic.Create(
                Analyzer.AccessInstanceNotFound,
                identifier.GetLocation(),
                identifier.ToString()
            );
            context.ReportDiagnostic(diagnostic);
            return;
        }

        var type = instanceType.MiddlePath('<', '>');

        if (string.IsNullOrEmpty(type))
        {
            var diagnostic = Diagnostic.Create(
                Analyzer.AccessInstanceUnknownType,
                identifier.GetLocation(),
                identifier.ToString()
            );
            context.ReportDiagnostic(diagnostic);
            return;
        }

        var accessProperties = accessFieldInfos.Select(field =>
        {
            var fieldType = field.TypeName;
            var type2 = fieldType.MiddlePath('<', '>');

            if (!string.IsNullOrEmpty(type2))
                return AccessPropertyTemplate.Render(new { TypeName = type2, field.FieldName }, member => member.Name);

            {
                var diagnostic = Diagnostic.Create(
                    Analyzer.AccessFieldUnknownType,
                    identifier.GetLocation(),
                    identifier.ToString()
                );
                context.ReportDiagnostic(diagnostic);
                return null;
            }
        });

        var sourceCode = Template.Render(new
        {
            Namespace = namespaceName,
            ClassName = className,
            Usings = usings,
            TypeName = type,
            Properties = string.Join("\n\n", accessProperties)
        }, member => member.Name);

        if (sourceCode == null) return;
        context.AddSource(uniqueHintName, SourceText.From(sourceCode, Encoding.UTF8));
    }
}