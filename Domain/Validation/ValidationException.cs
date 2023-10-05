using System.Runtime.Serialization;

namespace Architect.DddEfDemo.DddEfDemo.Domain.Validation;

/// <summary>
/// <para>
/// An exception that indicates client error.
/// Contains a stable error code as well as a user-friendly message.
/// </para>
/// <para>
/// Outer layers or systems can rely on the error code.
/// For example, a presentation layer could provide message translations, or an external system could respond in a preprogrammed way to predefined errors.
/// </para>
/// </summary>
[Serializable]
public class ValidationException : Exception
{
	// Message is inherited

	/// <summary>
	/// The string representation of the error code.
	/// </summary>
	public string ErrorCode { get; }

	/// <summary>
	/// The body of the message, not prefixed by the error code.
	/// </summary>
	public string MessageBody { get; }

	/// <summary>
	/// Constructs a new instance with the given code.
	/// The string representation forms the body of the message.
	/// </summary>
	public ValidationException(Enum errorCode)
		: this(errorCode.ToString())
	{
		this.MessageBody = errorCode.ToString();
	}

	/// <summary>
	/// Constructs a new instance with the given code and base message.
	/// </summary>
	public ValidationException(Enum errorCode, string message)
		: this(errorCode.ToString(), message)
	{
	}

	/// <summary>
	/// Constructs a new instance with the given code, base message, and inner exception.
	/// </summary>
	public ValidationException(Enum errorCode, string message, Exception innerException)
		: this(errorCode.ToString(), message, innerException)
	{
	}

	/// <summary>
	/// Core constructor.
	/// </summary>
	private ValidationException(string errorCode, string? messageBody = null, Exception? innerException = null)
		: base(GetMessage(errorCode ?? throw new ArgumentNullException(nameof(errorCode)), messageBody), innerException)
	{
		this.ErrorCode = errorCode;
		this.MessageBody = messageBody ?? this.ErrorCode;
	}

	private static string GetMessage(string errorCode, string? messageBody = null)
	{
		return messageBody is null
			? $"{errorCode}: {errorCode}."
			: $"{errorCode}: {messageBody}"; // The message body generally consists of one or more full sentences, i.e. with their own trailing dot
	}

	#region Serialization

	protected ValidationException(SerializationInfo info, StreamingContext context)
		: base(info, context)
	{
		this.ErrorCode = info.GetString("ErrorCode") ?? throw new IOException("Failed to deserialize: ErrorCode is missing.");
		this.MessageBody = info.GetString("MessageBody") ?? this.ErrorCode;
	}

	public override void GetObjectData(SerializationInfo info, StreamingContext context)
	{
		base.GetObjectData(info, context);

		info.AddValue("ErrorCode", this.ErrorCode);
		info.AddValue("MessageBody", this.MessageBody);
	}

	#endregion
}
