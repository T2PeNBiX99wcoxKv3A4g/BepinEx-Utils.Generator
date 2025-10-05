using System;
using System.IO;
using System.Reflection;

namespace BepInExUtils.Generator.Extensions;

public static class AssemblyExtensions
{
    /// <summary>
    ///     Retrieves the content of an embedded resource from an assembly as a string.
    /// </summary>
    /// <param name="assembly">The assembly containing the embedded resource.</param>
    /// <param name="resource">The name of the embedded resource.</param>
    /// <returns>The content of the embedded resource as a string.</returns>
    /// <exception cref="Exception">Thrown when the specified embedded resource is not found in the assembly.</exception>
    public static string GetEmbeddedResource(this Assembly assembly, string resource)
    {
        using var resourceStream = new StreamReader(assembly.GetManifestResourceStream(resource) ??
                                                    throw new(
                                                        $"Failed to find EmbeddedResource '{resource}' in Assembly '{assembly}' (Available Resources: {string.Join(", ", assembly.GetManifestResourceNames())})"));
        return resourceStream.ReadToEnd();
    }
}