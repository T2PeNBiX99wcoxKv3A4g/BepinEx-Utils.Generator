using System.Reflection;
using BepInExUtils.Generator.Extensions;

namespace BepInExUtils.Generator.AccessExtensions;

internal static class Resources
{
    private const string AccessExtensionsTemplatePath =
        "BepInExUtils.Generator.AccessExtensions.AccessExtensionsTemplate.scriban";

    private const string AccessFieldTemplatePath =
        "BepInExUtils.Generator.AccessExtensions.AccessFieldTemplate.scriban";

    private const string AccessPropertyTemplatePath =
        "BepInExUtils.Generator.AccessExtensions.AccessPropertyTemplate.scriban";

    private const string AccessMethodTypeTemplatePath =
        "BepInExUtils.Generator.AccessExtensions.AccessMethodTypeTemplate.scriban";

    private const string AccessMethodVoidTemplatePath =
        "BepInExUtils.Generator.AccessExtensions.AccessMethodVoidTemplate.scriban";

    public static readonly string AccessExtensionsTemplate =
        Assembly.GetExecutingAssembly().GetEmbeddedResource(AccessExtensionsTemplatePath);

    public static readonly string AccessFieldTemplate =
        Assembly.GetExecutingAssembly().GetEmbeddedResource(AccessFieldTemplatePath);

    public static readonly string AccessPropertyTemplate =
        Assembly.GetExecutingAssembly().GetEmbeddedResource(AccessPropertyTemplatePath);

    public static readonly string AccessMethodTypeTemplate =
        Assembly.GetExecutingAssembly().GetEmbeddedResource(AccessMethodTypeTemplatePath);

    public static readonly string AccessMethodVoidTemplate =
        Assembly.GetExecutingAssembly().GetEmbeddedResource(AccessMethodVoidTemplatePath);
}