using System.IO;
using System.Reflection;

namespace BepInExUtils.Generator.Extensions;

public static class AssemblyExtensions
{
    extension(Assembly assembly)
    {
        public string GetEmbeddedResource(string resource)
        {
            using var resourceStream = new StreamReader(assembly.GetManifestResourceStream(resource) ??
                                                        throw new(
                                                            $"Failed to find EmbeddedResource '{resource}' in Assembly '{assembly}' (Available Resources: {string.Join(", ", assembly.GetManifestResourceNames())})"));
            return resourceStream.ReadToEnd();
        }
    }
}