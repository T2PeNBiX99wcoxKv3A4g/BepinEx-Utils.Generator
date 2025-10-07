using System.Reflection;
using BepInExUtils.Generator.Extensions;

namespace BepInExUtils.Generator.AccessExtensions;

internal static class Resources
{
    private const string AccessExtensionsTemplatePath =
        "BepInExUtils.Generator.AccessExtensions.AccessExtensionsTemplate.scriban";

    private const string AccessPropertyTemplatePath =
        "BepInExUtils.Generator.AccessExtensions.AccessPropertyTemplate.scriban";

    public static readonly string AccessExtensionsTemplate =
        Assembly.GetExecutingAssembly().GetEmbeddedResource(AccessExtensionsTemplatePath);

    public static readonly string AccessPropertyTemplate =
        Assembly.GetExecutingAssembly().GetEmbeddedResource(AccessPropertyTemplatePath);
}