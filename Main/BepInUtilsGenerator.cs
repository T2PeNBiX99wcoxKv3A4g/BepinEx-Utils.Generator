using System.Linq;
using System.Text;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using Scriban;

namespace BepInExUtils.Generator.Main;

[Generator]
public class BepInUtilsGenerator : IIncrementalGenerator
{
    private const string BepInUtilsAttributeFullName = "BepInExUtils.Attributes.BepInUtilsAttribute";
    private const string BepInUtilsAttributeShortName = "BepInUtils";
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

    private static BepInPluginInfo? Transform(GeneratorAttributeSyntaxContext syntaxContext,
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
        var configBindDatas = syntaxContext.Attributes.Where(attr =>
            attr.AttributeClass?.Name.StartsWith(ConfigBindAttributeClassName + "<") ?? false);

        var attributeSyntax = syntax.AttributeLists.SelectMany(a => a.Attributes)
            .FirstOrDefault(attr => attr.Name.ToString() == BepInUtilsAttributeShortName);

        if (attributeSyntax?.ArgumentList == null) return null;

        var configs = syntax.AttributeLists.SelectMany(a => a.Attributes)
            .Where(attr => attr.Name.ToString().StartsWith(ConfigBindAttributeShortName + "<"))
            .Select(attr => (
                Name: attr.Name.ToString(),
                Arguments: attr.ArgumentList?.Arguments.Select(a => a.ToString()).ToArray() ?? []
            ))
            .ToList();

        var configValues = configBindDatas.Select(attr => attr?.ConstructorArguments ?? []).ToList();
        var argsConstant = bepInUtilsDatas?.ConstructorArguments ?? [];
        var args = attributeSyntax.ArgumentList.Arguments;
        var guid = argsConstant.Length > 0 && argsConstant[0].Kind == TypedConstantKind.Primitive &&
                   argsConstant[0].Value is string value
            ? value
            : args[0].ToString().Trim('"');
        var name = argsConstant.Length > 1 && argsConstant[1].Kind == TypedConstantKind.Primitive &&
                   argsConstant[1].Value is string value2
            ? value2
            : args[1].ToString().Trim('"');
        var version =
            argsConstant.Length > 2 && argsConstant[2].Kind == TypedConstantKind.Primitive &&
            argsConstant[2].Value is string value3
                ? value3
                : args[2].ToString().Trim('"');
        var classInfo = new ClassInfo(namespaceName, className, usingsText);
        var configInfos = configs.Select(val =>
        {
            var typeStartIndex = val.Name.IndexOf('<');
            var typeEndIndex = val.Name.IndexOf('>');
            var type = val.Name.Substring(typeStartIndex + 1, typeEndIndex - typeStartIndex - 1);
            var key = val.Arguments[0].Trim('"');
            var section = val.Arguments[1] ?? "\"Options\"";
            var defaultValue = val.Arguments[2] ?? "null";
            var description = val.Arguments[3] ?? "null";
            var minValue = val.Arguments.Length > 4 ? val.Arguments[4] : null;
            var maxValue = val.Arguments.Length > 5 ? val.Arguments[5] : null;

            return new ConfigInfo(type, key, section, defaultValue, description, minValue, maxValue);
        }).ToList();

        // var configValueInfos = configValues.Select(val =>
        // {
        //     var key = val.Length > 0 && val[0].Kind == TypedConstantKind.Primitive && val[0].Value is string value4
        //         ? value4
        //         : null;
        //     var section = val.Length > 1 && val[1].Kind == TypedConstantKind.Primitive && val[1].Value is string value5
        //         ? value5
        //         : null;
        //     var configDefinition =
        //         val.Length > 0 && val[0].Kind == TypedConstantKind.Type &&
        //         val[0].Value is ConfigDefinition configDefinitionValue
        //             ? configDefinitionValue
        //             : null;
        //     var defaultValue = val.Length > 2 && val[2].Kind == TypedConstantKind.Primitive ? val[2].Value : null;
        //     var defaultValue2 = val.Length > 1 && val[1].Kind == TypedConstantKind.Primitive ? val[1].Value : null;
        //     var description =
        //         val.Length > 3 && val[3].Kind == TypedConstantKind.Primitive && val[3].Value is string value6
        //             ? value6
        //             : null;
        //     var description2 =
        //         val.Length > 3 && val[3].Kind == TypedConstantKind.Primitive &&
        //         val[3].Value is ConfigDescription configDescriptionValue
        //             ? configDescriptionValue
        //             : null;
        //     description2 ??=
        //         val.Length > 2 && val[2].Kind == TypedConstantKind.Primitive &&
        //         val[2].Value is ConfigDescription configDescriptionValue2
        //             ? configDescriptionValue2
        //             : null;
        //
        //     return new ConfigValueInfo(key, section, configDefinition, defaultValue, defaultValue2, description,
        //         description2);
        // }).ToList();

        return new BepInPluginInfo(classInfo, guid, name, version, configInfos);
    }

    private static void Execute(SourceProductionContext context, BepInPluginInfo? info)
    {
        if (!info.HasValue) return;
        var (classInfo, guid, name, version, configInfos) = info.Value;
        var (namespaceName, className, usings) = classInfo;
        var uniqueHintName = classInfo.UniqueHintName;
        // var configInfoPairs = configInfos.Zip(configValueInfos, (configInfo, configValue) => (configInfo, configValue))
        //     .ToList();
        // var configInfoHandles = new List<ConfigInfoHandle>();
        //
        // for (var i = 0; i < configInfos.Count; i++)
        // {
        //     var configInfo = configInfos[i];
        //     var configValue = configValueInfos[i];
        //     var type = configInfo.Type;
        //     var key = configValue.Key ?? configValue.ConfigDefinition?.Key ?? configInfo.Key;
        //     var arg0 = configInfo.OtherArgs.Length > 0 ? configInfo.OtherArgs[0] : null;
        //     var arg1 = configInfo.OtherArgs.Length > 1 ? configInfo.OtherArgs[1] : null;
        //     var arg2 = configInfo.OtherArgs.Length > 2 ? configInfo.OtherArgs[2] : null;
        //     var section = configValue.Section ?? configValue.ConfigDefinition?.Section ?? arg1 ?? arg0;
        //     var defaultValue = configValue.DefaultValue ?? configValue.DefaultValue2 ?? arg2 ?? arg1;
        //     object? description = configValue.Description;
        //     description ??= configValue.ConfigDescription;
        //     description ??= arg2;
        //     
        //     configInfoHandles.Add(new(type, key, section, defaultValue?.ToString() ?? "null", description));
        // }

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