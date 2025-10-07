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
    private const string BepInUtilsAttributeShortName = "BepInUtils";
    internal const string BepInUtilsAttributeClassName = "BepInUtilsAttribute";
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

    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var classToGenerate =
            context.SyntaxProvider.ForAttributeWithMetadataName(BepInUtilsAttributeFullName, Predicate,
                Transform).Where(i => i is not null);

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

        var attributeSyntax = syntax.AttributeLists.SelectMany(a => a.Attributes)
            .FirstOrDefault(attr =>
                attr.Name.ToString() == BepInUtilsAttributeShortName ||
                attr.Name.ToString() == BepInUtilsAttributeClassName);

        if (attributeSyntax?.ArgumentList == null) return null;

        var configs = syntax.AttributeLists.SelectMany(a => a.Attributes)
            .Where(attr => attr.Name.ToString().StartsWith(ConfigBindAttributeShortName + '<') ||
                           attr.Name.ToString() == ConfigBindAttributeClassName)
            .Select(attr => (
                Name: attr.Name.ToString(),
                Arguments: attr.ArgumentList?.Arguments.Select(a => a.ToString()).ToArray() ?? []
            ))
            .ToList();

        var argsConstant = bepInUtilsDatas?.ConstructorArguments ?? [];
        var args = attributeSyntax.ArgumentList.Arguments;
        var guid = argsConstant.TryGetArg<string>(0) ?? args[0].ToString().Trim('"');
        var name = argsConstant.TryGetArg<string>(1) ?? args[1].ToString().Trim('"');
        var version = argsConstant.TryGetArg<string>(2) ?? args[2].ToString().Trim('"');
        var classInfo = new ClassInfo(namespaceName, className, usingsText, nameof(Generator),
            syntax.Identifier);
        var configInfos = configs.Select(val =>
        {
            var typeStartIndex = val.Name.IndexOf('<');
            var typeEndIndex = val.Name.IndexOf('>');
            var type = val.Name.Substring(typeStartIndex + 1, typeEndIndex - typeStartIndex - 1);
            var key = val.Arguments[0].Trim('"');
            var section = val.Arguments[1] ?? "\"Options\"";
            var defaultValue = val.Arguments[2] ?? "null";
            var description = val.Arguments[3] ?? "null";
            var minValue = val.Arguments.TryGet(4);
            var maxValue = val.Arguments.TryGet(5);

            return new ConfigInfo(type, key, section, defaultValue, description, minValue, maxValue);
        }).ToList();

        return new BepInUtilsInfo(classInfo, guid, name, version, configInfos);
    }

    private static void Execute(SourceProductionContext context, BepInUtilsInfo? info)
    {
        if (!info.HasValue) return;
        var (classInfo, guid, name, version, configInfos) = info.Value;
        var (namespaceName, className, usings, generatorName, _) = classInfo;
        var uniqueHintName = $"{namespaceName}.{className}_{generatorName}.generated.cs";
        var configFields = configInfos.Select(config =>
            ConfigFieldTemplate.Render(new { config.Type, config.Key }, member => member.Name)).ToList();
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