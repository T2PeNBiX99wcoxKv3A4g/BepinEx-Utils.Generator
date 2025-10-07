using System.Reflection;
using BepInExUtils.Generator.Extensions;

namespace BepInExUtils.Generator.BepInUtils;

internal static class Resources
{
    private const string BepInUtilsTemplatePath = "BepInExUtils.Generator.BepInUtils.BepInUtilsTemplate.scriban";
    private const string ConfigFieldTemplatePath = "BepInExUtils.Generator.BepInUtils.ConfigFieldTemplate.scriban";
    private const string ConfigValueTemplatePath = "BepInExUtils.Generator.BepInUtils.ConfigValueTemplate.scriban";

    private const string ConfigPropertyTemplatePath =
        "BepInExUtils.Generator.BepInUtils.ConfigPropertyTemplate.scriban";

    public static readonly string BepInUtilsTemplate =
        Assembly.GetExecutingAssembly().GetEmbeddedResource(BepInUtilsTemplatePath);

    public static readonly string ConfigFieldTemplate =
        Assembly.GetExecutingAssembly().GetEmbeddedResource(ConfigFieldTemplatePath);

    public static readonly string ConfigValueTemplate =
        Assembly.GetExecutingAssembly().GetEmbeddedResource(ConfigValueTemplatePath);

    public static readonly string ConfigPropertyTemplate =
        Assembly.GetExecutingAssembly().GetEmbeddedResource(ConfigPropertyTemplatePath);
}