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
    private const string ConfigBindAttributeShortName = "ConfigBind";
    private const string ConfigFieldTemplateString = "internal static ConfigEntry<{{Type}}>? _{{Key}};";

    private const string ConfigValueTemplateString =
        "Configs._{{Key}} = Config.Bind({{Section}}, nameof(Configs._{{Key}}), {{DefaultValue}}, {{Description}});";

    private const string ConfigPropertyTemplateString = """
                                                        public static {{Type}} {{Key}}
                                                        {
                                                            get => _{{Key}}?.Value ?? default;
                                                            set
                                                            {
                                                                if (_{{Key}} == null) return;
                                                                _{{Key}}.Value = value;
                                                            }
                                                        }
                                                        """;

    private static Template? _cacheTemplate;
    private static Template? _cacheConfigFieldTemplate;
    private static Template? _cacheConfigValueTemplate;
    private static Template? _cacheConfigPropertyTemplate;

    private static Template Template => _cacheTemplate ??= Template.Parse(Resources.BepInUtilsTemplate);

    private static Template ConfigFieldTemplate =>
        _cacheConfigFieldTemplate ??= Template.Parse(ConfigFieldTemplateString);

    private static Template ConfigValueTemplate =>
        _cacheConfigValueTemplate ??= Template.Parse(ConfigValueTemplateString);

    private static Template ConfigPropertyTemplate =>
        _cacheConfigPropertyTemplate ??= Template.Parse(ConfigPropertyTemplateString);

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

        var args = attributeSyntax.ArgumentList.Arguments;
        if (args.Count != 3) return null;
        var guid = args[0].ToString().Trim('"');
        var name = args[1].ToString().Trim('"');
        var version = args[2].ToString().Trim('"');
        var classInfo = new ClassInfo(namespaceName, className, usingsText);
        var configInfos = configs.Select(val =>
        {
            var typeStartIndex = val.Name.IndexOf('<');
            var typeEndIndex = val.Name.IndexOf('>');
            var type = val.Name.Substring(typeStartIndex + 1, typeEndIndex - typeStartIndex - 1);
            var key = val.Arguments[0].Trim('"');
            var otherArgs = val.Arguments.Skip(1).ToArray();

            return new ConfigInfo(type, key, otherArgs);
        }).ToList();

        return new BepInPluginInfo(classInfo, guid, name, version, configInfos);
    }

    private static void Execute(SourceProductionContext context, BepInPluginInfo? info)
    {
        if (!info.HasValue) return;
        var (classInfo, guid, name, version, configs) = info.Value;
        var (namespaceName, className, usings) = classInfo;
        var uniqueHintName = classInfo.UniqueHintName;
        var configFields = configs.Select(config =>
            ConfigFieldTemplate.Render(new { config.Type, config.Key }, member => member.Name)).ToList();
        var configPropertyList = configs.Select(config =>
            ConfigPropertyTemplate.Render(new { config.Type, config.Key }, member => member.Name)).ToList();
        var configValues = configs.Select(config => ConfigValueTemplate.Render(new
        {
            config.Type,
            config.Key,
            Section = config.OtherArgs[0],
            DefaultValue = config.OtherArgs[1],
            Description = config.OtherArgs[2]
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