using RevolutionShared.Rose.Data.Items.Equipment;
using System;

[AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
public sealed class STBReaderAttribute : Attribute
{
    public Type DataType { get; }

    public STBReaderAttribute(Type dataType)
    {
        if (!typeof(EquipmentData).IsAssignableFrom(dataType))
        {
            throw new ArgumentException($"{dataType.Name} must inherit from EquipmentData.");
        }

        DataType = dataType;
    }
}