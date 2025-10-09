using System.Collections.Generic;
using JetBrains.Annotations;

namespace BepInExUtils.Generator.Extensions;

public static class ListExtensions
{
    extension<T>(IList<T> list)
    {
        [UsedImplicitly]
        public bool TryGetValue(int index, out T? value)
        {
            if (index < 0 || index >= list.Count)
            {
                value = default;
                return false;
            }

            value = list[index];
            return true;
        }

        [UsedImplicitly]
        public T? GetValueOrDefault(int index) => list.TryGetValue(index, out var value) ? value : default;

        [UsedImplicitly]
        public bool TrySetValue(int index, T value)
        {
            if (index < 0 || index >= list.Count) return false;
            list[index] = value;
            return true;
        }
    }

    extension<T>(T[] array)
    {
        [UsedImplicitly]
        public bool TryGetValue(int index, out T? value)
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
        public T? GetValueOrDefault(int index) => array.TryGetValue(index, out var value) ? value : default;

        [UsedImplicitly]
        public bool TrySetValue(int index, T value)
        {
            if (index < 0 || index >= array.Length) return false;
            array[index] = value;
            return true;
        }
    }
}