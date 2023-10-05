namespace Architect.DddEfDemo.DddEfDemo.Domain;

/// <summary>
/// Tracks the creation of certain interesting <see cref="IDomainObject"/>s.
/// </summary>
public static class DomainObjectTracker
{
	/// <summary>
	/// Fired whenever a new <see cref="IDomainEvent"/> is created.
	/// </summary>
	public static event Action<IDomainEvent>? DomainEventCreated;
	/// <summary>
	/// Fired whenever a new orphaned <see cref="IDomainObject"/> is created, such as an <see cref="IDomainEvent"/> or other unrooted object that is produced as a side-effect.
	/// </summary>
	public static event Action<IDomainObject>? OrphanedDomainObjectCreated;

	public static void DidCreateOrphanedDomainObject(IDomainObject domainObject)
	{
		if (domainObject is IDomainEvent domainEvent)
			DomainEventCreated?.Invoke(domainEvent);

		OrphanedDomainObjectCreated?.Invoke(domainObject);
	}
}
