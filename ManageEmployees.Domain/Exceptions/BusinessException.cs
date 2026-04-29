using ManageEmployees.Domain.Models;
using System.Diagnostics;
using System.Runtime.Serialization;

namespace ManageEmployees.Domain.Exceptions
{
    [Serializable]
    public class BusinessException : Exception
    {
        public string? TraceId { get; set; }
        public List<Error> Errors { get; set; } = [];

        public BusinessException(string message) : base(message)
        {
            TraceId = Activity.Current?.Id;
        }

        public BusinessException(string message, List<Error> errors) : base(message)
        {
            TraceId = Activity.Current?.Id;
            Errors = errors;
        }

        public BusinessException(string? message, Exception? innerException) : base(message, innerException)
        {
            TraceId = Activity.Current?.Id;
        }

#pragma warning disable SYSLIB0051
        protected BusinessException(SerializationInfo info, StreamingContext context) : base(info, context) { }
#pragma warning restore SYSLIB0051
    }
}
