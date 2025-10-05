using System.Reflection;
using BepInExUtils.Generator.Extensions;

namespace BepInExUtils.Generator.Main;

internal static class Resources
{
    private const string BepInUtilsTemplatePath = "BepInExUtils.Generator.Main.BepInUtilsTemplate.scriban";

    public static readonly string BepInUtilsTemplate =
        Assembly.GetExecutingAssembly().GetEmbeddedResource(BepInUtilsTemplatePath);
}