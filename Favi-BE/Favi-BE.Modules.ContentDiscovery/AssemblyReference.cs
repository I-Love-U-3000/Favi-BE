using System.Reflection;

namespace Favi_BE.Modules.ContentDiscovery;

public static class AssemblyReference
{
    public static readonly Assembly Assembly = typeof(AssemblyReference).Assembly;
}
