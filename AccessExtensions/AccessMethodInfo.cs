using System.Collections.Generic;

namespace BepInExUtils.Generator.AccessExtensions;

public readonly record struct AccessMethodInfo(string TypeName, string? Name, List<string> OtherArgs)
{
    public readonly string? Name = Name;
    public readonly List<string> OtherArgs = OtherArgs;
    public readonly string TypeName = TypeName;
}