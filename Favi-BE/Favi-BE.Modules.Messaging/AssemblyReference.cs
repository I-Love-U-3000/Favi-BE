using System.Reflection;

namespace Favi_BE.Modules.Messaging;

public static class AssemblyReference
{
    public static readonly Assembly Assembly = typeof(AssemblyReference).Assembly;
}
