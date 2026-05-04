#if !NET

namespace System
{
    internal readonly struct Index(int value, bool fromEnd = false)
    {
        public int GetOffset(int length) => fromEnd ? length - value : value;
        public static implicit operator Index(int value) => new(value);
    }

    internal readonly struct Range(Index start, Index end)
    {
        public Index Start { get; } = start;
        public Index End { get; } = end;
    }
}

namespace System.Diagnostics.CodeAnalysis
{
    [AttributeUsage(AttributeTargets.Parameter)]
    internal sealed class NotNullWhenAttribute(bool returnValue) : Attribute
    {
        public bool ReturnValue { get; } = returnValue;
    }

    [AttributeUsage(
        AttributeTargets.Field | AttributeTargets.Parameter | AttributeTargets.Property,
        AllowMultiple = false, Inherited = false)]
    internal sealed class StringSyntaxAttribute(string syntax) : Attribute
    {
        public string Syntax { get; } = syntax;
        public const string NumericFormat = nameof(NumericFormat);
    }
}

#endif
