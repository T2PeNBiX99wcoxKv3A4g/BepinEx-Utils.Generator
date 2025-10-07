using System.Collections;
using System.Collections.Generic;
using JetBrains.Annotations;

namespace BepInExUtils.Generator.Extensions;

public static class ListExtensions
{
    extension<T>(IList<T> list)
    {
        [UsedImplicitly]
        public bool TryGet(int index, out T? value)
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
        public T? TryGet(int index) => index < 0 || index >= list.Count ? default : list[index];

        [UsedImplicitly]
        public bool TrySet(int index, T value)
        {
            if (index < 0 || index >= list.Count) return false;
            list[index] = value;
            return true;
        }
    }

    extension(IList list)
    {
        [UsedImplicitly]
        public bool TryGet(int index, out object? value)
        {
            if (index < 0 || index >= list.Count)
            {
                value = null;
                return false;
            }

            value = list[index];
            return true;
        }

        [UsedImplicitly]
        public object? TryGet(int index) => index < 0 || index >= list.Count ? null : list[index];

        [UsedImplicitly]
        public bool TrySet(int index, object value)
        {
            if (index < 0 || index >= list.Count) return false;
            list[index] = value;
            return true;
        }
    }

    extension<T>(IReadOnlyList<T> list)
    {
        [UsedImplicitly]
        public bool TryGet(int index, out T? value)
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
        public T? TryGet(int index) => index < 0 || index >= list.Count ? default : list[index];
    }

    extension<T>(T[] array)
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
        public T? TryGet(int index) => index < 0 || index >= array.Length ? default : array[index];

        [UsedImplicitly]
        public bool TrySet(int index, T value)
        {
            if (index < 0 || index >= array.Length) return false;
            array[index] = value;
            return true;
        }
    }
}