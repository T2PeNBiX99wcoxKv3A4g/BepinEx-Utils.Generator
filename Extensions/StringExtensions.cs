using System;
using JetBrains.Annotations;

namespace BepInExUtils.Generator.Extensions;

public static class StringExtensions
{
    extension(string str)
    {
        [UsedImplicitly]
        public string? FirstPath(char value)
        {
            var findEnd = str.LastIndexOf(value);
            return findEnd < 0 ? null : str.Substring(0, findEnd);
        }

        [UsedImplicitly]
        public string? FirstPath(string value)
        {
            var findEnd = str.LastIndexOf(value, StringComparison.Ordinal);
            return findEnd < 0 ? null : str.Substring(0, findEnd);
        }

        [UsedImplicitly]
        public string? LastPath(char value)
        {
            var findStart = str.IndexOf(value);
            return findStart < 0 ? null : str.Substring(findStart + 1);
        }

        [UsedImplicitly]
        public string? LastPath(string value)
        {
            var findStart = str.IndexOf(value, StringComparison.Ordinal);
            return findStart < 0 ? null : str.Substring(findStart + 1);
        }

        [UsedImplicitly]
        public string? MiddlePath(char first, char last) => str.FirstPath(last)?.LastPath(first);

        [UsedImplicitly]
        public string? MiddlePath(string first, string last) => str.FirstPath(last)?.LastPath(first);

        [UsedImplicitly]
        public string? MiddlePath(char first, string last) => str.FirstPath(last)?.LastPath(first);

        [UsedImplicitly]
        public string? MiddlePath(string first, char last) => str.FirstPath(last)?.LastPath(first);
    }
}