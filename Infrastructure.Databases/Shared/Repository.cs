namespace Architect.DddEfDemo.DddEfDemo.Infrastructure.Databases.Shared;

/// <summary>
/// Abstract base class for a repository.
/// </summary>
internal abstract class Repository<TEntity>
	where TEntity : class
{
	/// <summary>
	/// <para>
	/// The <see cref="IQueryable{T}"/> used to query the entire aggregate, including all its default includes.
	/// </para>
	/// <para>
	/// All entity-returning methods should query based on this property, because aggregates must be loaded in their entirety.
	/// </para>
	/// </summary>
	protected abstract IQueryable<TEntity> AggregateQueryable { get; }

	/// <summary>
	/// Retrieves and returns the current ambient DbContext, generally provided by use case, from the application layer.
	/// </summary>
	protected CoreDbContext DbContext => this.DbContextAccessor.CurrentDbContext;

	private IDbContextAccessor<CoreDbContext> DbContextAccessor { get; }

	protected Repository(IDbContextAccessor<CoreDbContext> dbContextAccessor)
	{
		this.DbContextAccessor = dbContextAccessor;
	}
}
