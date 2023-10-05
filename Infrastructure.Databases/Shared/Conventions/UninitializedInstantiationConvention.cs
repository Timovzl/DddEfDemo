using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.Serialization;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Metadata.Conventions;
using Microsoft.EntityFrameworkCore.Metadata.Internal;

namespace Architect.DddEfDemo.DddEfDemo.Infrastructure.Databases.Shared.Conventions;

/// <summary>
/// A convention that instantiates objects using <see cref="FormatterServices.GetUninitializedObject"/>, bypassing constructors.
/// This avoids the need to add default constructors and the risk that parameterized constructors are inadvertently used.
/// </summary>
internal sealed class UninitializedInstantiationConvention : IModelFinalizingConvention
{
	public void ProcessModelFinalizing(IConventionModelBuilder modelBuilder, IConventionContext<IConventionModelBuilder> context)
	{
		foreach (var entityType in modelBuilder.Metadata.GetEntityTypes())
		{
			if (entityType.ClrType.IsAbstract)
				continue;

#pragma warning disable EF1001 // Internal EF Core API usage -- EF demands usable constructors, even if we would use an interceptor that prevents their usage entirely
			var underlyingEntityType = entityType as EntityType ?? throw new NotImplementedException("Internal changes to the EF Core API have broken this code. Are public methods now available to configure instantiation?");
			underlyingEntityType.ConstructorBinding = new UninitializedBinding(entityType.ClrType);
#pragma warning restore EF1001 // Internal EF Core API usage
		}
	}

	/// <summary>
	/// An <see cref="InstantiationBinding"/> that produces uninitialized objects.
	/// </summary>
	private sealed class UninitializedBinding : InstantiationBinding
	{
		private static readonly MethodInfo GetUninitializedObjectMethod = typeof(FormatterServices).GetMethod(nameof(FormatterServices.GetUninitializedObject))!;

		public override Type RuntimeType { get; }

		public UninitializedBinding(Type runtimeType)
			: base(Array.Empty<ParameterBinding>())
		{
			this.RuntimeType = runtimeType;
		}

		public override Expression CreateConstructorExpression(ParameterBindingInfo bindingInfo)
		{
			return Expression.Convert(
				Expression.Call(method: GetUninitializedObjectMethod, arguments: Expression.Constant(this.RuntimeType)),
				this.RuntimeType);
		}

		public override InstantiationBinding With(IReadOnlyList<ParameterBinding> parameterBindings)
		{
			return this;
		}
	}
}
