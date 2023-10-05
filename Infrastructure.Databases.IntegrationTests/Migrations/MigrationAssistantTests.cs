namespace Architect.DddEfDemo.DddEfDemo.Infrastructure.Databases.IntegrationTests.Migrations;

public class MigrationAssistantTests : IntegrationTestBase
{
	[Fact]
	public async Task MigrateAsync_Regularly_ShouldHaveExpectedEffect()
	{
		this.ShouldCreateDatabase = false;

		var instance = this.Host.Services.GetRequiredService<MigrationAssistant<CoreDbContext>>();

		await instance.MigrateAsync(CancellationToken.None);

		Assert.NotEqual(0, Convert.ToInt32(await this.ExecuteScalar("SELECT COUNT(*) FROM __EFMigrationsHistory;")));
	}
}
