using System;

[AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
public sealed class ContractFromAttribute : Attribute
{
    public Type SourceType { get; }

    public ContractFromAttribute(Type sourceType)
    {
        SourceType = sourceType;
    }
}
