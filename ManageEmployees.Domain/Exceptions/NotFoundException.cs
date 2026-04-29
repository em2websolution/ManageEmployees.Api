using System.Runtime.Serialization;

namespace ManageEmployees.Domain.Exceptions;

[Serializable]
public class NotFoundException : BusinessException
{
    public NotFoundException(string message) : base(message) { }

#pragma warning disable SYSLIB0051
    protected NotFoundException(SerializationInfo info, StreamingContext context) : base(info, context) { }
#pragma warning restore SYSLIB0051
}
