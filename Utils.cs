using JetBrains.Annotations;
using Microsoft.CodeAnalysis;

namespace BepInExUtils.Generator;

public static class Utils
{
    // I know this is a bit weird. the Rider seem to don't support debug yet.
    [UsedImplicitly]
    public static void DebugWsg(Location location, string message) =>
        Diagnostic.Create(Analyzer.Test, location, message);
}