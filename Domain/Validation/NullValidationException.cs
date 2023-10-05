using System.Runtime.CompilerServices;
using System.Runtime.Serialization;

namespace Architect.DddEfDemo.DddEfDemo.Domain.Validation;

/// <summary>
/// Similar to an <see cref="ArgumentNullException"/>, except in the form of a <see cref="ValidationException"/>.
/// </summary>
[Serializable]
public class NullValidationException : ValidationException
{
	public string ParameterName { get; }

	public NullValidationException(Enum errorCode, string parameterName, [CallerFilePath] string? callerFilePath = null, [CallerMemberName] string? callerMemberName = null)
		: this(errorCode, parameterName, message: CreateErrorMessage(parameterName: parameterName, callerFilePath: callerFilePath, callerMemberName: callerMemberName))
	{
	}

	public NullValidationException(Enum errorCode, string parameterName, string message)
		: base(errorCode, message)
	{
		this.ParameterName = parameterName ?? throw new ArgumentNullException(nameof(parameterName));
	}

	private static string CreateErrorMessage(string? parameterName, string? callerFilePath, string? callerMemberName)
	{
		if (callerMemberName == ".ctor" && callerFilePath?.EndsWith(".cs") == true)
			parameterName = $"{Path.GetFileNameWithoutExtension(callerFilePath)} {parameterName}";

		return $"The following required data was missing: {parameterName}.";
	}

	#region Serialization

	protected NullValidationException(SerializationInfo info, StreamingContext context)
		: base(info, context)
	{
		this.ParameterName = info.GetString("ParameterName") ?? throw new IOException("Failed to deserialize: ParameterName is missing.");
	}

	public override void GetObjectData(SerializationInfo info, StreamingContext context)
	{
		base.GetObjectData(info, context);

		info.AddValue("ParameterName", this.ParameterName);
	}

	#endregion
}
