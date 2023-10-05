namespace Architect.DddEfDemo.DddEfDemo.Application.UnitTests;

public class UseCaseTests
{
	[Fact]
	public void UseCaseClasses_Always_ShouldBeApplicationServices()
	{
		var useCaseClasses = typeof(ApplicationRegistrationExtensions).Assembly.GetTypes()
			.Where(type => type.Name.EndsWith("UseCase") && type.IsClass);

		foreach (var type in useCaseClasses)
		{
			Assert.True(type.GetInterface("IApplicationService") is not null, $"{type.Name} should implement {nameof(IApplicationService)}.");
		}
	}
}
