namespace BedayaGroup.Application.Common.Exceptions;

public class NotFoundException : Exception
{
    public NotFoundException(string message) : base(message)
    {
    }

    public NotFoundException(string name, object key)
        : base($"العنصر '{name}' بالرمز المرجعي ({key}) غير موجود.")
    {
    }
}
