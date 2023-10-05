using Architect.DddEfDemo.DddEfDemo.Domain.Quotations;

namespace Architect.DddEfDemo.DddEfDemo.Infrastructure.Databases.Repositories;

internal sealed class QuotationRepo : Repository<Quotation>
{
	protected override IQueryable<Quotation> AggregateQueryable => this.DbContext.Set<Quotation>();

	public QuotationRepo(IDbContextAccessor<CoreDbContext> dbContextAccessor)
		: base(dbContextAccessor)
	{
	}

	public async Task<IReadOnlyList<Quotation>> ListAll(CancellationToken cancellationToken = default)
	{
		var result = await this.AggregateQueryable
			.ToListAsync(cancellationToken);

		return result;
	}
}
