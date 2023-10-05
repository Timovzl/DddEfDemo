using System.Collections.Concurrent;
using System.Runtime.CompilerServices;
using Architect.DomainModeling;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Architect.DddEfDemo.DddEfDemo.Domain;

namespace Architect.DddEfDemo.DddEfDemo.Infrastructure.Databases.Interceptors;

/// <summary>
/// When changes are being saved by a <see cref="CoreDbContext"/>, this interceptor enforces that any newly produced domain objects have been tracked, i.e. are no longer orphaned.
/// This prevents neglecting to insert such objects, particularly when saving their parent entity's modifications, which could otherwise lead to data inconsistencies.
/// </summary>
internal sealed class OrphanedDomainObjectInterceptor : ISaveChangesInterceptor
{
	private static ConditionalWeakTable<DbContext, ConcurrentQueue<IDomainObject>> UnsavedDomainObjectsPerDbContext { get; } = new ConditionalWeakTable<DbContext, ConcurrentQueue<IDomainObject>>();

	private bool IsEnabled { get; set; } = true;

	static OrphanedDomainObjectInterceptor()
	{
		CoreDbContext.DbContextDisposed += dbContext => UnsavedDomainObjectsPerDbContext.Remove(dbContext);
		DomainObjectTracker.OrphanedDomainObjectCreated += TrackUnsavedDomainObject;

		// Local function that tracks a given unsaved domain object if there currently is an ambient DbContext
		static void TrackUnsavedDomainObject(IDomainObject domainObject)
		{
			if (!DbContextScope<CoreDbContext>.HasDbContext)
				return;

			var dbContext = DbContextScope<CoreDbContext>.CurrentDbContext;
			var unsavedDomainObjects = UnsavedDomainObjectsPerDbContext.GetOrCreateValue(dbContext);
			unsavedDomainObjects.Enqueue(domainObject);
		}
	}

	/// <summary>
	/// Disables the interceptor until further notice.
	/// </summary>
	public void Disable()
	{
		this.IsEnabled = false;
	}

	/// <summary>
	/// Enables the interceptor.
	/// If an ambient <see cref="DbContext"/> exists, all domain objects tracked under it are removed, providing a fresh start.
	/// </summary>
	public void Enable()
	{
		this.IsEnabled = true;

		try
		{
			var dbContext = DbContextScope<CoreDbContext>.CurrentDbContext;
			UnsavedDomainObjectsPerDbContext.Remove(dbContext);
		}
		catch (InvalidOperationException)
		{
			// No ambient DbContext
		}
	}

	/// <summary>
	/// <para>
	/// If the current object is enabled, this method throws if there are orphaned domain objects that should have been tracked by <paramref name="dbContext"/> but were not.
	/// </para>
	/// <para>
	/// This method should be called <em>after</em> saving (but before committing), not before saving.
	/// This is because a new entity added to the navigation property of an existing entity is not immediately tracked, but will be tracked during saving.
	/// </para>
	/// </summary>
	private void PreventOrphanedDomainObjects(DbContext dbContext)
	{
		if (!this.IsEnabled || !UnsavedDomainObjectsPerDbContext.TryGetValue(dbContext, out var unsavedDomainObjects))
			return;

		foreach (var domainObject in unsavedDomainObjects)
		{
			if (dbContext.Entry(domainObject).State != EntityState.Unchanged)
				throw new InvalidOperationException($"Saved changes without adding the newly created domain object to the change tracker: {domainObject}.");
		}

		UnsavedDomainObjectsPerDbContext.Remove(dbContext);
	}

	/// <summary>
	/// Intercepts just after saving changes synchronously, supposedly before committing.
	/// </summary>
	public int SavedChanges(SaveChangesCompletedEventData eventData, int result)
	{
		if (eventData.Context is not null)
			this.PreventOrphanedDomainObjects(eventData.Context);

		return result;
	}

	/// <summary>
	/// Intercepts just before saving changes asynchronously, supposedly before committing.
	/// </summary>
	public ValueTask<int> SavedChangesAsync(SaveChangesCompletedEventData eventData, int result, CancellationToken cancellationToken = default)
	{
		if (eventData.Context is not null)
			this.PreventOrphanedDomainObjects(eventData.Context);

		return new ValueTask<int>(result);
	}

	#region Unused

	public InterceptionResult<int> SavingChanges(DbContextEventData eventData, InterceptionResult<int> result)
	{
		// Nothing to do
		return result;
	}

	public ValueTask<InterceptionResult<int>> SavingChangesAsync(DbContextEventData eventData, InterceptionResult<int> result, CancellationToken cancellationToken = default)
	{
		// Nothing to do
		return new ValueTask<InterceptionResult<int>>(result);
	}

	public void SaveChangesFailed(DbContextErrorEventData eventData)
	{
		// Nothing to do
	}

	public Task SaveChangesFailedAsync(DbContextErrorEventData eventData, CancellationToken cancellationToken = default)
	{
		// Nothing to do
		return Task.CompletedTask;
	}

	#endregion
}
