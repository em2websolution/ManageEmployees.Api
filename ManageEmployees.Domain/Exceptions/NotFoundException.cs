namespace ManageEmployees.Domain.Exceptions;

[Serializable]
public class NotFoundException : BusinessException
{
    public NotFoundException(string message) : base(message) { }
}
