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

namespace BepInExUtils.Generator.BepInUtils;

[Generator]
public class BepInUtilsGenerator : IIncrementalGenerator
{
    internal const string BepInUtilsAttributeFullName = "BepInExUtils.Attributes.BepInUtilsAttribute";
    private const string BepInUtilsAttributeClassName = "BepInUtilsAttribute";
    private const string ConfigBindAttributeShortName = "ConfigBind";
    private const string ConfigBindAttributeClassName = "ConfigBindAttribute";

    private static Template? _cacheTemplate;
    private static Template? _cacheConfigFieldTemplate;
    private static Template? _cacheConfigValueTemplate;
    private static Template? _cacheConfigPropertyTemplate;

    private static Template Template => _cacheTemplate ??= Template.Parse(Resources.BepInUtilsTemplate);

    private static Template ConfigFieldTemplate =>
        _cacheConfigFieldTemplate ??= Template.Parse(Resources.ConfigFieldTemplate);

    private static Template ConfigValueTemplate =>
        _cacheConfigValueTemplate ??= Template.Parse(Resources.ConfigValueTemplate);

    private static Template ConfigPropertyTemplate =>
        _cacheConfigPropertyTemplate ??= Template.Parse(Resources.ConfigPropertyTemplate);

#if DEBUG
    private static readonly List<string> DebugOutput = [];
    private const bool Debug = false;

    private static void DebugMsg(string msg) => DebugOutput.Add(msg);
#else
    private static void DebugMsg(string msg) {}
#endif

    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var classToGenerate =
            context.SyntaxProvider.ForAttributeWithMetadataName(BepInUtilsAttributeFullName, Predicate,
                Transform).Where(i => i is not null);

#if DEBUG
        if (Debug)
            context.RegisterPostInitializationOutput(callback =>
            {
                callback.AddSource("test.g.cs", string.Join("\n", DebugOutput));
            });
#endif

        context.RegisterSourceOutput(classToGenerate, Execute);
    }

    private static bool Predicate(SyntaxNode syntaxNode, CancellationToken cancellationToken) =>
        syntaxNode is ClassDeclarationSyntax syntax
        && syntax.Modifiers.Any(SyntaxKind.PartialKeyword)
        && !syntax.Modifiers.Any(SyntaxKind.StaticKeyword);

    private static BepInUtilsInfo? Transform(GeneratorAttributeSyntaxContext syntaxContext,
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

        var bepInUtilsDatas =
            syntaxContext.Attributes.FirstOrDefault(attr => attr.AttributeClass?.Name == BepInUtilsAttributeClassName);

        var configs = syntax.AttributeLists.SelectMany(a => a.Attributes)
            .Where(attr => attr.Name.ToString().StartsWith(ConfigBindAttributeShortName + '<') ||
                           attr.Name.ToString() == ConfigBindAttributeClassName)
            .Select(attr => (
                Name: attr.Name.ToString(),
                Arguments: attr.ArgumentList?.Arguments.Select(a => a.ToString()).ToArray() ?? []
            ))
            .ToList();

        var configArgs = syntaxContext.TargetSymbol.GetAttributes()
            .Where(attr => attr.AttributeClass?.Name == ConfigBindAttributeClassName)
            .Select(data => data.ConstructorArguments).ToList();
        var configKeys = configArgs.Select(args => args.GetArgOrDefault<string>(0)).ToList();

        var argsConstant = bepInUtilsDatas?.ConstructorArguments ?? [];
        var guid = argsConstant.GetArgOrDefault<string>(0);
        var name = argsConstant.GetArgOrDefault<string>(1);
        var version = argsConstant.GetArgOrDefault<string>(2);
        var classInfo = new ClassInfo(namespaceName, className, usingsText, nameof(Generator),
            syntax.Identifier);
        var configInfos = configs.Zip(configKeys, (val, key) =>
        {
            var type = val.Name.MiddlePath('<', '>');
            var section = val.Arguments.GetValueOrDefault(1) ?? "\"Options\"";
            var defaultValue = val.Arguments.GetValueOrDefault(2) ?? "null";
            var description = val.Arguments.GetValueOrDefault(3) ?? "null";
            var minValue = val.Arguments.GetValueOrDefault(4);
            var maxValue = val.Arguments.GetValueOrDefault(5);

            return new ConfigInfo(type, key, section, defaultValue, description, minValue, maxValue);
        }).ToList();

        return new BepInUtilsInfo(classInfo, guid, name, version, configInfos);
    }

    private static void Execute(SourceProductionContext context, BepInUtilsInfo? info)
    {
        if (!info.HasValue) return;
        var (classInfo, guid, name, version, configInfos) = info.Value;
        var (namespaceName, className, usings, generatorName, identifier) = classInfo;
        var uniqueHintName = $"{namespaceName}.{className}_{generatorName}.generated.cs";
        // ReSharper disable once InvertIf
        var configFields = configInfos.Select(config =>
        {
            if (string.IsNullOrEmpty(config.Key))
            {
                var diagnostic = Diagnostic.Create(
                    Analyzer.EmptyName,
                    identifier.GetLocation(),
                    identifier.ToString(),
                    ConfigBindAttributeShortName
                );
                context.ReportDiagnostic(diagnostic);
                return null;
            }

            return ConfigFieldTemplate.Render(new { config.Type, config.Key }, member => member.Name);
        }).ToList();
        var configPropertyList = configInfos.Select(config =>
            ConfigPropertyTemplate.Render(new { config.Type, config.Key }, member => member.Name)).ToList();
        var configValues = configInfos.Select(config => ConfigValueTemplate.Render(new
        {
            config.Type,
            config.Key,
            config.Section,
            config.DefaultValue,
            config.Description,
            AcceptableValueRange = config.MinValue != null
                ? $"new AcceptableValueRange<{config.Type}>({config.MinValue}, {config.MaxValue})"
                : "null"
        }, member => member.Name)).ToList();

        if (guid is null || name is null || version is null)
        {
            var diagnostic = Diagnostic.Create(
                Analyzer.NullReferenceInBepInUtils,
                identifier.GetLocation(),
                identifier.ToString()
            );
            context.ReportDiagnostic(diagnostic);
        }

        var sourceCode = Template.Render(new
        {
            Namespace = namespaceName,
            ClassName = className,
            Usings = usings,
            Guid = guid,
            Name = name,
            Version = version,
            ConfigFields = string.Join("\n", configFields),
            ConfigPropertyList = string.Join("\n\n", configPropertyList),
            ConfigValues = string.Join("\n", configValues)
        }, member => member.Name);

        if (sourceCode == null) return;
        context.AddSource(uniqueHintName, SourceText.From(sourceCode, Encoding.UTF8));
    }
}