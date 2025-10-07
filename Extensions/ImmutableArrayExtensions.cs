using System.Collections.Immutable;
using JetBrains.Annotations;
using Microsoft.CodeAnalysis;

namespace BepInExUtils.Generator.Extensions;

public static class ImmutableArrayExtensions
{
    extension<T>(ImmutableArray<T> array)
    {
        [UsedImplicitly]
        public bool TryGet(int index, out T? value)
        {
            if (index < 0 || index >= array.Length)
            {
                value = default;
                return false;
            }

            value = array[index];
            return true;
        }

        [UsedImplicitly]
        public T? TryGet(int index) => index >= array.Length ? default : array[index];
    }

    extension(ImmutableArray<TypedConstant> array)
    {
        [UsedImplicitly]
        public bool TryGetArg<T>(int index, out T? value)
        {
            if (!array.TryGet(index, out var typedConstant) || typedConstant is not T getValue)
            {
                value = default;
                return false;
            }

            value = getValue;
            return true;
        }

        [UsedImplicitly]
        public T? TryGetArg<T>(int index)
        {
            if (!array.TryGet(index, out var typedConstant) || typedConstant is not T getValue) return default;
            return getValue;
        }
    }
}