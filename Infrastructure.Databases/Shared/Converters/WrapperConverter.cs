using System.Linq.Expressions;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.Serialization;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Architect.DddEfDemo.DddEfDemo.Infrastructure.Databases.Shared.Converters;

/// <summary>
/// <para>
/// Converts between types <typeparamref name="TModel"/> and <typeparamref name="TProvider"/> without using constructors, where <typeparamref name="TModel"/> is a type containing a single (non-public) instance field of type <typeparamref name="TProvider"/>.
/// </para>
/// <para>
/// This type primarily is primarily intended for converting between wrapper value objects and primitives.
/// </para>
/// <para>
/// Unlike <see cref="CastingConverter{TModel, TProvider}"/>, the current converter actively avoids constructors.
/// For example, if a domain rule in <typeparamref name="TModel"/>'s constructor changes but existing data is allowed to remain unchanged, the constructor might refuse to load such data from the database.
/// </para>
/// </summary>
internal sealed class WrapperConverter<TModel, TProvider> : ValueConverter<TModel, TProvider>
{
	private delegate void FieldAssignmentFunction(ref TModel instance, TProvider value);

	private static readonly Lazy<FieldAssignmentFunction> FieldSetter = new Lazy<FieldAssignmentFunction>(
		() => CreateFieldSetter(GetExpectedField()),
		LazyThreadSafetyMode.ExecutionAndPublication);

	private static MethodInfo GetUninitializedObjectMethod => typeof(FormatterServices).GetMethod(nameof(FormatterServices.GetUninitializedObject))!;

	public WrapperConverter()
		: base(CreateConversionToProviderExpression(), CreateConversionToModelExpression())
	{
	}

	private static FieldInfo GetExpectedField()
	{
		var field = typeof(TModel).GetFields(BindingFlags.Instance | BindingFlags.NonPublic).SingleOrDefault(field => field.FieldType == typeof(TProvider)) ??
			throw new ArgumentException($"Type {typeof(TModel).Name} cannot be used for wrapping conversions, because it does not have exactly 1 (non-public) instance field of type {typeof(TProvider).Name}.");
		return field;
	}

	private static Expression<Func<TModel, TProvider>> CreateConversionToProviderExpression()
	{
		var field = GetExpectedField();

		// TModel instance;
		var param = Expression.Parameter(typeof(TModel), "instance");

		// return instance.Field;
		var fieldAccess = Expression.Field(param, field);

		var conversion = Expression.Lambda<Func<TModel, TProvider>>(fieldAccess, param);
		return conversion;
	}

	private static Expression<Func<TProvider, TModel>> CreateConversionToModelExpression()
	{
		// TProvider value;
		var param = Expression.Parameter(typeof(TProvider), "value");

		// TModel instance;
		var instanceVariable = Expression.Variable(typeof(TModel), "instance");

		var block = Expression.Block(
			variables: new[] { instanceVariable },
			expressions: new Expression[]
			{
				// instance = default(TModel);
				// -- or --
				// instance = (TModel)FormatterServices.GetUninitializedObject(typeof(TModel));
				typeof(TModel).IsValueType
					? Expression.Assign(instanceVariable, Expression.Constant(default(TModel)))
					: Expression.Assign(instanceVariable, Expression.Convert(
						Expression.Call(GetUninitializedObjectMethod, arguments: Expression.Constant(typeof(TModel))),
						typeof(TModel))),

				// SetField(ref instance, value);
				Expression.Invoke(Expression.Constant(FieldSetter.Value), instanceVariable, param),

				// return instance;
				instanceVariable,
			});

		var conversion = Expression.Lambda<Func<TProvider, TModel>>(block, param);
		return conversion;
	}

	/// <summary>
	/// <para>
	/// For a particular field, this compiles a new function that writes that field for a given instance, assigning a given value.
	/// </para>
	/// <para>
	/// Supports structs. Supports readonly fields.
	/// </para>
	/// </summary>
	private static FieldAssignmentFunction CreateFieldSetter(FieldInfo field)
	{
		// We must write IL to achieve the following:
		// - Write to readonly fields
		// - Mutate structs in-place (instead of the normal semantics of mutating a copy, which would not help us)

		var setter = new DynamicMethod(
			name: $"SetFieldValue_{field.DeclaringType?.Name}_{field.Name}",
			returnType: typeof(void),
			parameterTypes: new[] { typeof(TModel).MakeByRefType(), typeof(TProvider), },
			m: typeof(WrapperConverter<TModel, TProvider>).Module,
			skipVisibility: true);

		var ilGenerator = setter.GetILGenerator();

		ilGenerator.Emit(OpCodes.Ldarg_0); // Load the reference to the instance to write to

		if (!typeof(TModel).IsValueType)
			ilGenerator.Emit(OpCodes.Ldind_Ref); // For a reference type, dereference the double indirection

		ilGenerator.Emit(OpCodes.Ldarg_1); // Load the value to be assigned
		ilGenerator.Emit(OpCodes.Stfld, field); // Assign the new value to the instance's field
		ilGenerator.Emit(OpCodes.Ret); // Return

		return setter.CreateDelegate<FieldAssignmentFunction>();
	}
}
