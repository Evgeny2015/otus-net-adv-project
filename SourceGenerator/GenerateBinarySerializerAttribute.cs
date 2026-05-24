using System;

namespace SourceGenerator;

/// <summary>
/// Marks a class for binary serializer generation.
/// When applied to a class, the source generator will create a partial class
/// with a SerializeToBinary method that writes the object's properties to a stream.
/// </summary>
[AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
public sealed class GenerateBinarySerializerAttribute : Attribute
{
    /// <summary>
    /// Initializes a new instance of the <see cref="GenerateBinarySerializerAttribute"/> class.
    /// </summary>
    public GenerateBinarySerializerAttribute()
    {
    }
}