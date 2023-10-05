using Architect.DddEfDemo.DddEfDemo.Application.Shared;

namespace Architect.DddEfDemo.DddEfDemo.Api.Mocks;

/// <summary>
/// <para>
/// Satisfies the requirement of an <see cref="IJobEnqueuer"/>, in case the DI container is validated in its entirety.
/// </para>
/// <para>
/// To support enqueuing jobs, Hangfire packages need to be installed into the project.
/// </para>
/// </summary>
public sealed class MockJobEnqueuer : IJobEnqueuer
{
	public Task EnqueueJob(string jobNamePrefix)
	{
		throw new NotImplementedException("The API does not currently have the ability to enqueue jobs. Hangfire connectivity would need to be deliberately installed first.");
	}

	public Task ScheduleJob(string jobNamePrefix, DateTimeOffset instant)
	{
		throw new NotImplementedException("The API does not currently have the ability to enqueue jobs. Hangfire connectivity would need to be deliberately installed first.");
	}
}
