using Architect.DddEfDemo.DddEfDemo.Infrastructure.Databases.Repositories;
using Architect.DddEfDemo.DddEfDemo.Testing.Common.Extensions;

namespace Architect.DddEfDemo.DddEfDemo.Infrastructure.Databases.IntegrationTests.Mappings;

public sealed class QuotationRepoTests : IntegrationTestBase
{
	[Fact]
	public async Task ListAll_AfterSaving_ShouldReturnExpectedResult()
	{
		var repo = this.Host.Services.GetRequiredService<QuotationRepo>();

		var entity = new QuotationDummyBuilder()
			.Build();

		using var arrangementScope = this.CreateDbContextScope();

		arrangementScope.DbContext.Add(entity);

		await arrangementScope.DbContext.SaveChangesAsync();
		await arrangementScope.DisposeAsync();

		using var assertionScope = this.CreateDbContextScope();

		var results = await repo.ListAll();

		var result = Assert.Single(results);
		Assert.Equal(entity.Id, result.Id);
		Assert.Equal(entity.CreationDateTime.Round(), result.CreationDateTime); // DateTimes get rounded by the database
		Assert.Equal(result.ExpirationDateTime?.Round(), result.ExpirationDateTime); // DateTimes get rounded by the database
		Assert.Equal(entity.IsExpired, result.IsExpired);
		Assert.Equal(entity.Lines, result.Lines);
		Assert.Equal(entity.PurchaseLines, result.PurchaseLines);
		Assert.Equal(entity.DiscountLines, result.DiscountLines);
	}
}
