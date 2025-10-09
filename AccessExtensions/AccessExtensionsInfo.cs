using System.Collections.Generic;

namespace BepInExUtils.Generator.AccessExtensions;

public readonly record struct AccessExtensionsInfo(
    ClassInfo ClassInfo,
    string? InstanceType,
    List<AccessFieldInfo> AccessFieldInfos,
    List<AccessPropertyInfo> AccessPropertyInfos,
    List<AccessMethodInfo> AccessMethodInfos)
{
    public readonly List<AccessFieldInfo> AccessFieldInfos = AccessFieldInfos;
    public readonly List<AccessMethodInfo> AccessMethodInfos = AccessMethodInfos;
    public readonly List<AccessPropertyInfo> AccessPropertyInfos = AccessPropertyInfos;
    public readonly ClassInfo ClassInfo = ClassInfo;
    public readonly string? InstanceType = InstanceType;
}