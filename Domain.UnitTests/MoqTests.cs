using Moq;

namespace Architect.DddEfDemo.DddEfDemo.Domain.UnitTests;

public sealed class MoqTests
{
	/// <summary>
	/// Moq versions beyond minor 4.18 do scary data collection and are worth avoiding.
	/// </summary>
	[Fact]
	public void MoqMajorVersion_Always_ShouldHaveExpectedValue()
	{
		var moqAssembly = typeof(Mock).Assembly;
		var moqVersion = moqAssembly.GetName().Version;

		Assert.Equal(4, moqVersion?.Major);
		Assert.Equal(18, moqVersion?.Minor);
	}
}
