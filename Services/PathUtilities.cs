using System;
using System.IO;
using System.Linq;

namespace CLASSIC.Core.IO;

public static class PathUtilities
{
    /// <summary>
    /// Gets a path relative to the executable's directory
    /// </summary>
    public static string GetRelativePath(params string[] paths)
    {
        var basePath = AppContext.BaseDirectory;
        return Path.Combine(new[] { basePath }.Concat(paths).ToArray());
    }
}