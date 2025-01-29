using Microsoft.AspNetCore.Mvc;
using ManageEmployees.Domain.Models;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using System.Net;
using System.Security.Authentication;

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
    }

    public static class ExceptionExtensions
    {
        public static ProblemDetails ToProblemDetails(this Exception e)
        {
            return new ProblemDetails()
            {
                Status = (int)GetErrorCode(e.InnerException ?? e),
                Title = e.Message
            };
        }

        private static HttpStatusCode GetErrorCode(Exception e)
        {
            switch (e)
            {
                case ValidationException _:
                case FormatException _:
                case BusinessException _:
                    return HttpStatusCode.BadRequest;
                case AuthenticationException _:
                    return HttpStatusCode.Forbidden;
                case NotImplementedException _:
                    return HttpStatusCode.NotImplemented;
                default:
                    return HttpStatusCode.InternalServerError;
            }
        }

    }
}
