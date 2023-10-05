using System.Linq.Expressions;
using Moq;
using Moq.Protected;

namespace Architect.DddEfDemo.DddEfDemo.Testing.Common.Http;

/// <summary>
/// Helps mock <see cref="IHttpClientFactory"/> and <see cref="HttpClientHandler"/>, to test HTTP interactions.
/// </summary>
public static class HttpClientFactoryMocker
{
	/// <summary>
	/// Returns a mock <see cref="IHttpClientFactory"/> and outputs a corresponding <see cref="HttpClientHandler"/> that can be used for setup and verification.
	/// </summary>
	public static Mock<IHttpClientFactory> CreateMockHttpClientFactory(out MockHttpClientHandler mockHttpClientHandler)
	{
		mockHttpClientHandler = new MockHttpClientHandler();
		mockHttpClientHandler.As<IDisposable>().Setup(h => h.Dispose());

		var handler = mockHttpClientHandler.Object;

		var mockHttpClientFactory = new Mock<IHttpClientFactory>(MockBehavior.Strict);
		mockHttpClientFactory
			.Setup(f => f.CreateClient(It.IsAny<string>()))
			.Returns(() => new HttpClient(handler))
			.Verifiable();

		return mockHttpClientFactory;
	}
}

public class MockHttpClientHandler : Mock<DelegatingHandler>
{
	/// <summary>
	/// Sets up the <see cref="HttpClientHandler.SendAsync"/> method to return the configured response.
	/// </summary>
	public void SetupSendAsync(HttpResponseMessage value)
	{
		this.SetupSendAsync(() => value);
	}

	/// <summary>
	/// Sets up the <see cref="HttpClientHandler.SendAsync"/> method to return the configured response if the given <paramref name="predicate"/> is matched.
	/// </summary>
	public void SetupSendAsync(Expression<Func<HttpRequestMessage, bool>> predicate, HttpResponseMessage value)
	{
		this.SetupSendAsync(predicate, () => value);
	}

	/// <summary>
	/// Sets up the <see cref="HttpClientHandler.SendAsync"/> method to return the configured response.
	/// </summary>s
	public void SetupSendAsync(Func<HttpResponseMessage> valueFunction)
	{
		this.SetupSendAsync(_ => valueFunction());
	}

	/// <summary>
	/// Sets up the <see cref="HttpClientHandler.SendAsync"/> method to return the configured response.
	/// </summary>s
	public void SetupSendAsync(Func<HttpRequestMessage, HttpResponseMessage> valueFunction)
	{
		this.Protected()
			.Setup<Task<HttpResponseMessage>>(nameof(HttpClient.SendAsync), ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
			.ReturnsAsync((HttpRequestMessage requestMessage, CancellationToken _) => valueFunction(requestMessage))
			.Verifiable();
	}

	/// <summary>
	/// Sets up the <see cref="HttpClientHandler.SendAsync"/> method to return the configured response if the given <paramref name="predicate"/> is matched.
	/// </summary>
	public void SetupSendAsync(Expression<Func<HttpRequestMessage, bool>> predicate, Func<HttpResponseMessage> valueFunction)
	{
		this.SetupSendAsync(predicate, _ => valueFunction());
	}

	/// <summary>
	/// Sets up the <see cref="HttpClientHandler.SendAsync"/> method to return the configured response if the given <paramref name="predicate"/> is matched.
	/// </summary>
	public void SetupSendAsync(Expression<Func<HttpRequestMessage, bool>> predicate, Func<HttpRequestMessage, HttpResponseMessage> valueFunction)
	{
		this.Protected()
			.Setup<Task<HttpResponseMessage>>(nameof(HttpClient.SendAsync), ItExpr.Is(predicate), ItExpr.IsAny<CancellationToken>())
			.ReturnsAsync((HttpRequestMessage requestMessage, CancellationToken _) => valueFunction(requestMessage))
			.Verifiable();
	}

	/// <summary>
	/// Verifies that <see cref="HttpClientHandler.SendAsync"/> was called.
	/// </summary>
	public void VerifySendAsync(Times times)
	{
		this.VerifySendAsync(times, _ => true);
	}

	/// <summary>
	/// Verifies that <see cref="HttpClientHandler.SendAsync"/> was called.
	/// </summary>
	public void VerifySendAsync(Times times, Expression<Func<HttpRequestMessage, bool>> predicate)
	{
		this.VerifySendAsync(times, ItExpr.Is(predicate), ItExpr.IsAny<CancellationToken>());
	}

	/// <summary>
	/// Verifies that <see cref="HttpClientHandler.SendAsync"/> was called.
	/// </summary>
	public void VerifySendAsync(Times times, params object[] args)
	{
		this.Protected().Verify(nameof(HttpClient.SendAsync), times, args);
	}
}
