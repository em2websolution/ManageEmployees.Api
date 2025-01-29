using Microsoft.AspNetCore.Http;
using Serilog.Context;

namespace ManageEmployees.Services.Settings
{
    public class LogSettings
    {
        private readonly RequestDelegate next;

        public LogSettings(RequestDelegate next)
        {
            this.next = next;
        }

        public Task Invoke(HttpContext context)
        {
            LogContext.PushProperty("UserIpAddress", context.Connection.RemoteIpAddress);
            LogContext.PushProperty("TimeStampUTC", DateTime.UtcNow);

            return next(context);
        }
    }
}
