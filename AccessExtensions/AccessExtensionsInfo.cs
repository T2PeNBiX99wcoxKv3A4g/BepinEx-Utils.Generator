using System.Collections.Generic;

namespace BepInExUtils.Generator.AccessExtensions;

public readonly record struct AccessExtensionsInfo(
    ClassInfo ClassInfo,
    string? InstanceType,
    List<AccessFieldInfo> AccessFieldInfos)
{
    public readonly List<AccessFieldInfo> AccessFieldInfos = AccessFieldInfos;
    public readonly ClassInfo ClassInfo = ClassInfo;
    public readonly string? InstanceType = InstanceType;
}