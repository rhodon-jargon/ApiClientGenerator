// ReSharper disable once CheckNamespace
namespace System;

public readonly struct Index
{
    private readonly int value;
    private readonly bool fromEnd;

    // To use the "hat" operator (^), the following is required:
    public Index(int value, bool fromEnd) {
        this.value = value;
        this.fromEnd = fromEnd;
    }

    // To use the System.Index type as an argument in an array element access, the following member is required:
    public int GetOffset(int length) => fromEnd ? length - value : value;
}