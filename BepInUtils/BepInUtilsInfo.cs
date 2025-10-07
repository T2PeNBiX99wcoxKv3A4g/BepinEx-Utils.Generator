using System.Collections.Generic;

namespace BepInExUtils.Generator.BepInUtils;

public readonly record struct BepInUtilsInfo(
    ClassInfo ClassInfo,
    string Guid,
    string Name,
    string Version,
    List<ConfigInfo> ConfigInfos)
{
    public readonly ClassInfo ClassInfo = ClassInfo;
    public readonly List<ConfigInfo> ConfigInfos = ConfigInfos;
    public readonly string Guid = Guid;
    public readonly string Name = Name;
    public readonly string Version = Version;
}