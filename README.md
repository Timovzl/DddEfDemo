# DddEfDemo

A demo showing how to use uncompromising Domain-Driven Design (DDD) with Entity Framework (EF) Core.

## Example Domain Model

- Seller is an entity with simple properties and a wrapper value object property.
- ProperName is (reusable) wrapper value object around a string.
- Entities have ID properties that are custom structs, each wrapping a 28-digit decimal with 0 decimal places (e.g. SellerId).
- Product is an entity that also groups a set of properties in a value object, ManufacturerDetails, persisted in-row.
- Quotation is an entity that features multiple calculated properties (not persisted) and a set of QuotationLines, the latter being persisted as a JSON blob.

## Steps

Once you have created an uncompromising domain model, use the following steps to map it using EF.

### Conventions

- Disable the booby traps of automatic column and relationship discovery. (CoreDbContext.cs)
- During entity reconstitution, avoid constructors. Rules for new instances may become be stricter than for existing ones. (CoreDbContext.cs; UninitializedInstantiationConvention.cs)
- For string-based properties, perform comparisons according to their column collations, to avoid inconsistencies between code and database. (CoreDbContext.cs; StringCasingConvention.cs)
- For wrapper value objects, convert to the underlying type automatically, and avoid constructors. Rules for new instances may become stricer than for existing ones. (CoreDbContext.cs; WrapperValueObjectConversionConvention.cs)
- Configure Date and DateTime mappings, particularly avoiding `DateTimeKind.Unspecified`. (CoreDbContext.cs; UtcDateTimeConverter.cs; DateOnlyConverter.cs)
- Configure the default precision of (non-ID) decimals, settling on one suitable for the entire bounded context. (CoreDbContext.cs)
- Configure conventions for reused value objects. (CoreDbContext.cs)

### Specifics

- Use a mapping class per entity. (CoreDbContext.cs; Mappings directory)
- For an entity property that is a value object to be stored in-row, use `OwnsOne()` and custom column names. (ProductMapping.cs)
- For an entity property that is a set of value objects to be stored as a one-to-many relationship, use `OwnsMany()` with an inline mapping of the child object. (QuotationMapping.cs)

## OwnsOne

- `OwnsOne()` has a few disadvantages:
	- EF will not let us reuse the same _instance_ of a value object on two entities, e.g. newProduct.Manufacturer = oldProduct.Manufacturer.
		- Requires annoying workaround copying the value object.
	- Adding seed data (i.e. initial database rows included in the migrations) for owned objects is extremely cumbersome.
- Simply replacing `OwnsOne()` by EF8's `ComplexProperty()` eliminates the disadvantages.
	- However, support is planned but delayed for optional (i.e. nullable) value objects on an entity.
	- However, support is planned but delayed for seeding value objects on an entity.

## Tools

- [Architect.DomainModeling](https://github.com/TheArchitectDev/Architect.DomainModeling): A complete Domain-Driven Design (DDD) toolset for implementing domain models, including base types and source generators.
- [Architect.Identities](https://github.com/TheArchitectDev/Architect.Identities): Auto-increment or UUID? The DistributedId is a UUID replacement that is generated on-the-fly (without orchestration), unique, hard to guess, easy to store and sort, and highly efficient as a database key.
- [Architect.Identities.EntityFramework](https://www.nuget.org/packages/Architect.Identities.EntityFramework): Extension methods for configuring decimal ID columns.
- [Scrutor](https://github.com/khellang/Scrutor): Dependency registration by convention through assembly scanning.

## Related Notes

Don't forget the following:

- When using aggregates, query with `Include(x => x.Child)` to populate the child entities. _Always_ include this as part of the base query, because incomplete aggregates violate DDD.
	- This can be done by using a repository that _always_ queries based on its own calculated property: `private IQueryable<Parent> AggregateQueryable => this.DbContext.Set<Parent>().Include(x => x.Child)`.
- For value objects mapped to JSON blobs, be sure to specify a value comparer. For collections, use `EnumerableComparer.EnumerableEquals` and `EnumerableComparer.GetEnumerableHashCode` to get structural equality. (QuotationMapping.cs)
- Avoid concurrency conflicts when running migrations, by either migrating from a pipeline or using careful locking techniques. (MigrationAssistant.cs)
- Use optimistic concurrency control for entities, to avoid the "lost update problem". (SellerMapping.cs)
- When storing decimal values, avoid silent truncation by the database, by either using a value object that restricts the precision or using a decimal-to-decimal "conversion" that throws if too much precision is observed.
	- [Attributes](https://github.com/Timovzl/SolutionTemplate/tree/master/ToDoBoundedContextName/Domain/Shared) can be used to distinguish between monetary amounts vs. non-monetary decimals vs. exceptions that _do_ allow silent truncation, with separate conversions for each.
- Provide entities with a _single_ ID if possible. Each additional ID on an entity reduces clarity. (Domain project; CoreDbContext.cs)
- Be mindful of collations, and set a sensible default in the database and EF. (CoreDbDContext.cs; 00010101000000_DatabaseCollation.cs)
- Pluralize table names consistently. (CoreDbContext.cs)
- If possible, in DDD, give entities a single ID that is both publicly usable (when necessary) and efficient as a primary key.
	- A solution that provides the best of both worlds is the DistributedId from [Architect.Identities](https://github.com/TheArchitectDev/Architect.Identities).
- A DbContext instance is a resource, not a dependency. Only services should be injected as dependencies, and they should aim to be stateless. Blazor Server and Blazor United make this quite clear.
	- EF's [DbContextFactory](https://learn.microsoft.com/en-us/ef/core/dbcontext-configuration/#using-a-dbcontext-factory-eg-for-blazor) is a service that allows DbContexts to be produced and managed on-demand.
	- [Architect.EntityFramework.DbContextManagement](https://github.com/TheArchitectDev/Architect.EntityFramework.DbContextManagement) simplifies DbContext management.

## Domain Events

Domain events, especially persistend ones, can be a useful tool.
This is not to be confused with _event sourcing_, which is the practice of storing _only_ events, and replaying those to reconstruct state (such as entities).
Event sourcing is a practice that is extremely hard to make both easy to work with and performant.
However, there is an alternative that will give us most of the upsides without the downsides.

Instead of storing only a stream of events, we can store entities _and_ relevant domain events.
The entities provide easy access to the current state, whereas the events represent a full history of how we got to that state.

Clearly, this introduces some level of duplication.
Sure, the events have happened and are thus immutable.
However, we should cover the risk of mismatching changes: whenever an entity is added or modified, the corresponding events (where applicable) must be stored, and vice versa.

We can guarantee the above invariant quite well, provided that the data set is manipulated solely through the domain model using Entity Framework.
This is achieved as follows:

- Every entity uses optimistic concurrency control, by means of the `IsRowVersion()` feature. (SellerMapping.cs; ProductMapping.cs; QuotationMapping.cs)
- All sets of database writes are transactional, which can be as simple as using a single call to `SaveChanges()` at the end.
- Each domain event type inherits from `DomainEvent<TId>`. (DomainEvent.cs)
- Whenever a domain event is constructed, the base class broadcasts it as being initially "orphaned". (DomainObjectTracker.cs)
- The DbContext is equipped with an interceptor that subscribes to the event, tracking each orphaned domain event in relation to DbContext in whose execution context it was constructed. (OrphanedDomainObjectInterceptor.cs)
- When the DbContext's changes are saved, before they are committed, the interceptor looks at each orphaned domain event related to its DbContext, throwing if it was not added to the change tracker. (OrphanedDomainObjectInterceptor.cs)
- No change that produces events can be successfully saved unless its events are also saved in the same transaction.

The recommendation is to use `out` params in the domain model's event-producing methods, to confront the developer with the responsibility of adding them to the change tracker.
However, forgetting is no longer an issue, thanks to the failsafe above.

Note that the DbContexts can be scoped to their respective execution flows by using [Architect.EntityFramework.DbContextManagement](https://github.com/TheArchitectDev/Architect.EntityFramework.DbContextManagement).
