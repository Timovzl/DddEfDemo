using System.Reflection;

namespace Architect.DddEfDemo.DddEfDemo.Infrastructure.Databases.Shared;

internal static class TypeExtensions
{
	private static readonly BindingFlags BindingFlags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

	/// <summary>
	/// Returns the <paramref name="type"/>'s backing field for the auto-property with the given <paramref name="propertyName"/>.
	/// </summary>
	public static FieldInfo GetBackingField(this Type type, string propertyName)
	{
		var property = type.GetProperty(propertyName, BindingFlags) ??
			throw new ArgumentException($"Could not find property {propertyName} on {type.Name}.");
		var backingField = type.GetField(propertyName, BindingFlags | BindingFlags.IgnoreCase) ??
			type.GetField($"_{propertyName}", BindingFlags | BindingFlags.IgnoreCase) ??
			type.GetField($"<{property.Name}>k__BackingField", BindingFlags) ??
			throw new ArgumentException($"Could not find a backing field for property {propertyName} on {type.Name}.");
		return backingField;
	}
}
