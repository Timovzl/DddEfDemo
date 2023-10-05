using Architect.DddEfDemo.DddEfDemo.Application;
using Architect.DddEfDemo.DddEfDemo.Domain;
using Architect.DddEfDemo.DddEfDemo.Domain.Shared;
using Architect.Identities.EntityFramework;
using Microsoft.EntityFrameworkCore.Metadata.Conventions;

namespace Architect.DddEfDemo.DddEfDemo.Infrastructure.Databases;

/// <summary>
/// The DbContext for the bounded context's core database.
/// </summary>
internal sealed class CoreDbContext : DbContext, ICoreDatabase
{
	#region Always consider collations

	// UTF-8 (e.g. Latin1_General_100_BIN2_UTF8/Latin1_General_100_CI_AS_SC_UTF8) is avoided because its lengths are in bytes (easy to mismatch with validations in .NET) and it requires IsUnicode() and UseColumnType()

	/// <summary>
	/// Our preferred binary collation: a binary, case-sensitive collation that matches .NET's <see cref="StringComparison.Ordinal"/>.
	/// </summary>
	public const string BinaryCollation = "Latin1_General_100_BIN2";
	/// <summary>
	/// <para>
	/// Our preferred culture-sensitive collation: a culture-sensitive, ignore-case, accent-sensitive collation.
	/// </para>
	/// <para>
	/// Use this collation only for non-indexed (or at the very least non-FK) columns, such as titles and descriptions.
	/// </para>
	/// </summary>
	public const string CulturalCollation = "Latin1_General_100_CI_AS";
	/// <summary>
	/// Our default collation, used for textual columns that do not specify one.
	/// </summary>
	public const string DefaultCollation = BinaryCollation;

	#endregion

	#region Storing events

	/// <summary>
	/// Fired after a <see cref="CoreDbContext"/> instance is disposed.
	/// Note that the instance may have already been returned to the pool if <see cref="DbContext"/> pooling is enabled.
	/// </summary>
	internal static event Action<CoreDbContext>? DbContextDisposed;

	public CoreDbContext(DbContextOptions<CoreDbContext> options)
		: base(options)
	{
	}

	public override void Dispose()
	{
		base.Dispose();
		DbContextDisposed?.Invoke(this);
	}

	public override ValueTask DisposeAsync()
	{
		var result = base.DisposeAsync();
		DbContextDisposed?.Invoke(this);
		return result;
	}

	#endregion

	protected override void OnModelCreating(ModelBuilder modelBuilder)
	{
		base.OnModelCreating(modelBuilder);

		#region Set the default collation

		// Be sure to set the database to match this
		modelBuilder.UseCollation(DefaultCollation);

		#endregion

		#region Use a mapping class per entity

		modelBuilder.ApplyConfigurationsFromAssembly(typeof(CoreDbContext).Assembly);

		#endregion

		#region Pluralize table names

		this.PluralizeTableNames(modelBuilder);

		#endregion

		#region Add seed data

		Seeder.AddSeedData(modelBuilder);

		#endregion
	}

	protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
	{
		base.ConfigureConventions(configurationBuilder);

		#region Explicit column and relationship declarations

		// We map things explicitly and deliberately, to enable changes to our domain model without unexpected schema changes
		// Beware: EF still discovers unintended calculated properties containing types that EF knows about, requiring a manual .Ignore(), but at least it throws (for lack of a mapping) instead of making assumptions
		configurationBuilder.Conventions.Remove(typeof(RelationshipDiscoveryConvention));
		configurationBuilder.Conventions.Remove(typeof(PropertyDiscoveryConvention));

		#endregion

		#region Conventions for Entities+ValueObjects, strings, and WrapperValueObjects

		// For entities and value objects, avoid ctors (reconstitution should not involve logic)
		configurationBuilder.Conventions.Remove(typeof(ConstructorBindingConvention));
		configurationBuilder.Conventions.Add(_ => new UninitializedInstantiationConvention());

		// For string-based columns, compare their properties with case-sensitivity matching the columns
		configurationBuilder.Conventions.Add(_ => new StringCasingConvention());

		// For WrapperValueObjects, convert to/from their wrapped types and avoid ctors
		configurationBuilder.Conventions.Add(_ => new WrapperValueObjectConversionConvention());

		#endregion

		#region IDs

		// For ID types convertible to/from decimal, use an appropriate column type
		configurationBuilder.ConfigureDecimalIdTypes(typeof(DomainRegistrationExtensions).Assembly);

		#endregion

		#region Dates and times

		// EF8 should support Date out of the box
		configurationBuilder.Properties<DateOnly>()
			.HaveColumnType("date")
			.HaveConversion<DateOnlyConverter>();

		configurationBuilder.Properties<DateTime>()
			.HavePrecision(3)
			.HaveConversion<UtcDateTimeConverter>();

		#endregion

		#region Default decimal precision

		// Configure default precision for (non-ID) decimals outside of properties (e.g. in CAST(), SUM(), AVG(), etc.)
		configurationBuilder.DefaultTypeMapping<decimal>()
			.HasPrecision(19, 9);

		// Configure default precision for (non-ID) decimal properties
		configurationBuilder.Properties<decimal>()
			.HavePrecision(19, 9);

		#endregion

		#region Shared WrapperValueObjects

		// Configure a common wrapper value object by convention
		configurationBuilder.Properties<ProperName>()
			//.HaveConversion<WrapperConverter<ProperName, string>>() // Already done by WrapperValueObjectConversionConvention above
			.HaveMaxLength(ProperName.MaxLength)
			.UseCollation(CulturalCollation);

		// Configure a common wrapper value object by convention
		configurationBuilder.Properties<ExternalId>()
			//.HaveConversion<WrapperConverter<ExternalId, string>>() // Already done by WrapperValueObjectConversionConvention above
			.HaveMaxLength(ExternalId.MaxLength)
			.UseCollation(BinaryCollation);

		#endregion
	}

	#region Table naming convention

	/// <summary>
	/// Ensures that table names are in plural.
	/// Although EF does this automatically where our <see cref="DbSet{TEntity}"/>s are named this way, entities without one (i.e. non-roots) require manual intervention.
	/// </summary>
	private void PluralizeTableNames(ModelBuilder modelBuilder)
	{
		foreach (var entityType in modelBuilder.Model.GetEntityTypes().Where(entityType => !entityType.IsOwned() && entityType.ClrType is not null))
		{
			var clrTypeName = entityType.ClrType!.Name;

			entityType.SetTableName(clrTypeName.EndsWith('y')
				? $"{clrTypeName[..^1]}ies"
				: clrTypeName.EndsWith('s')
				? $"{clrTypeName}es"
				: $"{clrTypeName}s");
		}
	}

	#endregion
}
