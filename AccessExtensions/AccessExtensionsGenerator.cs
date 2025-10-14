using System.Collections.Generic;
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
    private const string AccessFieldAttributeClassName = "AccessFieldAttribute";
    private const string AccessPropertyAttributeShortName = "AccessProperty";
    private const string AccessPropertyAttributeClassName = "AccessPropertyAttribute";
    private const string AccessMethodAttributeShortName = "AccessMethod";
    private const string AccessMethodAttributeClassName = "AccessMethodAttribute";

    private static Template? _cacheTemplate;
    private static Template? _cacheAccessFieldTemplate;
    private static Template? _cacheAccessPropertyTemplate;
    private static Template? _cacheAccessMethodTypeTemplate;
    private static Template? _cacheAccessMethodVoidTemplate;

    private static Template Template => _cacheTemplate ??= Template.Parse(Resources.AccessExtensionsTemplate);

    private static Template AccessFieldTemplate =>
        _cacheAccessFieldTemplate ??= Template.Parse(Resources.AccessFieldTemplate);

    private static Template AccessPropertyTemplate =>
        _cacheAccessPropertyTemplate ??= Template.Parse(Resources.AccessPropertyTemplate);

    private static Template AccessMethodTypeTemplate =>
        _cacheAccessMethodTypeTemplate ??= Template.Parse(Resources.AccessMethodTypeTemplate);

    private static Template AccessMethodVoidTemplate =>
        _cacheAccessMethodVoidTemplate ??= Template.Parse(Resources.AccessMethodVoidTemplate);

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

        // Get field type names and arguments
        var accessFieldTypeNames = syntax.AttributeLists.SelectMany(a => a.Attributes)
            .Where(attr => attr.Name.ToString().StartsWith(AccessFieldAttributeShortName + '<') ||
                           attr.Name.ToString().StartsWith(AccessFieldAttributeClassName + '<'))
            .Select(attr => attr.Name.ToString()).ToList();

        var accessFieldArgs = syntaxContext.TargetSymbol.GetAttributes()
            .Where(attr => attr.AttributeClass?.Name is AccessFieldAttributeClassName)
            .Select(data => data.ConstructorArguments).ToList();
        var accessFieldNames = accessFieldArgs.Select(args => args.GetArgOrDefault<string>(0) ?? "").ToList();

        var accessFieldInfos = accessFieldTypeNames
            .Zip(accessFieldNames, (typeName, name) => new AccessFieldInfo(typeName, name)).ToList();

        // Get property type names and arguments
        var accessPropertyTypeNames = syntax.AttributeLists.SelectMany(a => a.Attributes)
            .Where(attr => attr.Name.ToString().StartsWith(AccessPropertyAttributeShortName + '<') ||
                           attr.Name.ToString().StartsWith(AccessPropertyAttributeClassName + '<'))
            .Select(attr => attr.Name.ToString()).ToList();

        var accessPropertyArgs = syntaxContext.TargetSymbol.GetAttributes()
            .Where(attr => attr.AttributeClass?.Name is AccessPropertyAttributeClassName)
            .Select(data => data.ConstructorArguments).ToList();
        var accessPropertyNames = accessPropertyArgs.Select(args => args.GetArgOrDefault<string>(0) ?? "").ToList();

        var accessPropertyInfos = accessPropertyTypeNames
            .Zip(accessPropertyNames, (typeName, name) => new AccessPropertyInfo(typeName, name)).ToList();

        // var test = syntaxContext.TargetSymbol.GetAttributes().Select(attr => attr.AttributeClass?.Name);

        // throw new NotImplementedException($"1:{string.Join(", ", accessFieldTypeNames)}");

        // Get method type names and arguments
        var accessMethodTypeNames = syntax.AttributeLists.SelectMany(a => a.Attributes)
            .Where(attr => attr.Name.ToString().StartsWith(AccessMethodAttributeShortName))
            .Select(attr =>
            {
                var args = attr.ArgumentList?.Arguments.Select(a => a.ToString()).ToList() ?? [];
                var otherArgs = args.Skip(1).ToList();

                return (TypeName: attr.Name.ToString(), OtherArgs: otherArgs);
            }).ToList();

        var accessMethodArgs = syntaxContext.TargetSymbol.GetAttributes()
            .Where(attr => attr.AttributeClass?.Name is AccessMethodAttributeClassName)
            .Select(data => data.ConstructorArguments).ToList();
        var accessMethodNames = accessMethodArgs.Select(args => args.GetArgOrDefault<string>(0) ?? "").ToList();

        var accessMethodInfos = accessMethodTypeNames.Zip(accessMethodNames,
            (tuple, name) => new AccessMethodInfo(tuple.TypeName, name, tuple.OtherArgs)).ToList();

        var classInfo = new ClassInfo(namespaceName, className, usingsText, nameof(AccessExtensionsGenerator),
            syntax.Identifier);
        return new AccessExtensionsInfo(classInfo, instanceType, accessFieldInfos, accessPropertyInfos,
            accessMethodInfos);
    }

    private static void Execute(SourceProductionContext context, AccessExtensionsInfo? info)
    {
        if (!info.HasValue) return;
        var (classInfo, instanceType, accessFieldInfos, accessPropertyInfos, accessMethodInfos) = info.Value;
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

        // ReSharper disable once InvertIf
        var accessFields = accessFieldInfos.Select(field =>
        {
            var fieldType = field.TypeName;
            var type2 = fieldType.MiddlePath('<', '>');

            if (string.IsNullOrEmpty(type2))
            {
                var diagnostic = Diagnostic.Create(
                    Analyzer.AccessInfoUnknownType,
                    identifier.GetLocation(),
                    identifier.ToString(),
                    AccessFieldAttributeShortName
                );
                context.ReportDiagnostic(diagnostic);
                return null;
            }

            if (string.IsNullOrEmpty(field.Name))
            {
                var diagnostic = Diagnostic.Create(
                    Analyzer.EmptyName,
                    identifier.GetLocation(),
                    identifier.ToString(),
                    AccessFieldAttributeShortName
                );
                context.ReportDiagnostic(diagnostic);
                return null;
            }

            return AccessFieldTemplate.Render(new
            {
                TypeName = type2,
                field.Name
            }, member => member.Name);
        });

        // ReSharper disable once InvertIf
        var accessProperties = accessPropertyInfos.Select(property =>
        {
            var propertyType = property.TypeName;
            var type2 = propertyType.MiddlePath('<', '>');

            if (string.IsNullOrEmpty(type2))
            {
                var diagnostic = Diagnostic.Create(
                    Analyzer.AccessInfoUnknownType,
                    identifier.GetLocation(),
                    identifier.ToString(),
                    AccessPropertyAttributeShortName
                );
                context.ReportDiagnostic(diagnostic);
                return null;
            }

            if (string.IsNullOrEmpty(property.Name))
            {
                var diagnostic = Diagnostic.Create(
                    Analyzer.EmptyName,
                    identifier.GetLocation(),
                    identifier.ToString(),
                    AccessPropertyAttributeShortName
                );
                context.ReportDiagnostic(diagnostic);
                return null;
            }

            return AccessPropertyTemplate.Render(new
                {
                    TypeName = type2,
                    property.Name
                },
                member => member.Name);
        });

        // ReSharper disable once InvertIf
        var accessMethods = accessMethodInfos.Select(method =>
        {
            var methodType = method.TypeName;
            var type2 = methodType.MiddlePath('<', '>');

            if (string.IsNullOrEmpty(method.Name))
            {
                var diagnostic = Diagnostic.Create(
                    Analyzer.EmptyName,
                    identifier.GetLocation(),
                    identifier.ToString(),
                    AccessMethodAttributeShortName
                );
                context.ReportDiagnostic(diagnostic);
                return null;
            }

            var argTypes = method.OtherArgs.Select(arg =>
            {
                var typeName = arg.Replace(" ", "").MiddlePath("typeof(", ")")?.Trim();

                if (string.IsNullOrEmpty(typeName))
                {
                    var diagnostic = Diagnostic.Create(
                        Analyzer.AccessInfoUnknownType,
                        identifier.GetLocation(),
                        identifier.ToString(),
                        AccessMethodAttributeShortName
                    );
                    context.ReportDiagnostic(diagnostic);
                    return "object";
                }

                return typeName ?? "object";
            }).ToList();

            var argumentsWithTypes = "";
            var arguments = "";
            var template = string.IsNullOrEmpty(type2) ? AccessMethodVoidTemplate : AccessMethodTypeTemplate;

            // ReSharper disable once InvertIf
            if (argTypes.Count > 0)
            {
                var argCountList = new Dictionary<string, int>();
                var argumentNames = argTypes.Select(arg =>
                    {
                        argCountList[arg] = argCountList.TryGetValue(arg, out var count) ? count + 1 : 1;
                        return (char.ToLower(arg.GetValueOrDefault(0) ?? 'a') + arg.Substring(1) + argCountList[arg])
                            .Replace(".", "_").Replace("<", "_").Replace(">", "_").Replace("[", "_").Replace("]", "_")
                            .Replace(",", "_").Replace("(", "_").Replace(")", "_").Replace(" ", "_");
                    })
                    .ToList();
                var argumentsWithTypesList = argTypes.Zip(argumentNames, (arg, name) => $"{arg} {name}").ToList();
                argumentsWithTypes = string.Join(", ", argumentsWithTypesList);
                arguments = ", " + string.Join(", ", argumentNames);
            }

            return template.Render(new
                {
                    TypeName = type2,
                    method.Name,
                    ArgumentsWithTypes = argumentsWithTypes,
                    Arguments = arguments
                },
                member => member.Name);
        });

        var sourceCode = Template.Render(new
        {
            Namespace = namespaceName,
            ClassName = className,
            Usings = usings,
            TypeName = type,
            Field = string.Join("\n\n", accessFields),
            Property = string.Join("\n\n", accessProperties),
            Methods = string.Join("\n\n", accessMethods)
        }, member => member.Name);

        if (sourceCode == null) return;
        context.AddSource(uniqueHintName, SourceText.From(sourceCode, Encoding.UTF8));
    }
}