#nullable disable // Disable nullable, to avoid giving the package consumer's compiler any indication of null vs. non-null, which are unclear with optionals

namespace Architect.DddEfDemo.DddEfDemo.Contracts;

/// <summary>
/// <para>
/// This type helps distinguish between the absence of a value versus a [possibly null] value.
/// </para>
/// <para>
/// A deliberately instantiated object contains a [possibly null] value, whereas a default instance has <em>no</em> value.
/// </para>
/// <para>
/// <see cref="GetValueOrProvided(T)"/> can be used to obtain either the contained value, or a given fallback.
/// </para>
/// </summary>
public readonly struct Optional<T>
{
	public override string ToString() => this.HasValue ? this.ValueOrDefault?.ToString() : "[Omitted Optional]";

	/// <summary>
	/// If true, this object contains a value (which might be null).
	/// If false, the object is uninitialized, i.e. "missing".
	/// </summary>
	public bool HasValue { get; }

	/// <summary>
	/// The contained value, which might be null, or the default value for <typeparamref name="T"/>.
	/// </summary>
	public T ValueOrDefault { get; }

	/// <summary>
	/// <para>
	/// Wraps the given value.
	/// </para>
	/// <para>
	/// Alternatively, a "missing" value can be constructed by passing <paramref name="hasValue"/>=false.
	/// </para>
	/// <para>
	/// Since values can be implicitly converted to <see cref="Optional{T}"/>, manually invoking this constructor is normally unnecessary.
	/// </para>
	/// </summary>
	/// <param name="value">If <paramref name="hasValue"/> is false, this parameter is ignored.</param>
	public Optional(T value, bool hasValue = true)
	{
		this.HasValue = hasValue;
		this.ValueOrDefault = hasValue ? value : default;
	}

	/// <summary>
	/// <para>
	/// Returns the contained value, which might be null.
	/// </para>
	/// <para>
	/// This method throws if <see cref="HasValue"/> is false.
	/// </para>
	/// </summary>
	public T GetValue()
	{
		return this.HasValue
			? this.ValueOrDefault
			: ThrowNoValue();
	}

	/// <summary>
	/// <para>
	/// If the <see cref="Optional{T}"/> was deliberately populated, this method returns its value (which might be null).
	/// </para>
	/// <para>
	/// A default <see cref="Optional{T}"/> instance results in the <paramref name="provided"/> value instead.
	/// </para>
	/// </summary>
	public T GetValueOrProvided(T provided)
	{
		var result = this.HasValue ? this.ValueOrDefault : provided;
		return result;
	}

	private static T ThrowNoValue()
	{
		throw new InvalidOperationException($"Attempted to obtain the value of an {nameof(Optional<object>)} that was not provided.");
	}

	public static implicit operator Optional<T>(T value) => new Optional<T>(value);
}
