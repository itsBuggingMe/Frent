
using System.Text;

namespace Frent.Generator;

public static class RegistryHelpers
{
    public static StringBuilder AppendNamespace(this StringBuilder sb, string @namespace)
    {
        if (@namespace == string.Empty)
            return sb;
        return sb.Append(@namespace).Append('.');
    }

    public const string UpdateTypeAttributeName = "Frent.Updating.UpdateTypeAttribute";
    public const string UpdateOrderInterfaceName = "Frent.Updating.IComponentUpdateOrderAttribute";
    public const string UpdateMethodName = "Update";
    public const string FileName = "ComponentUpdateTypeRegistry.g.cs";
    public const string TargetInterfaceName = "IComponentBase";
    public const string FullyQualifiedTargetInterfaceName = "Frent.Components.IComponentBase";
}
