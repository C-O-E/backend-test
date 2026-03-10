using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using System.Text.Json;

namespace BackTest.Filters;

/// <summary>
/// Translates <see cref="JsonException"/> (e.g. missing "type" discriminator
/// in a polymorphic request body) into a 400 Bad Request response instead of
/// 500 Internal Server Error.
/// </summary>
public class JsonExceptionFilter : IExceptionFilter
{
    public void OnException(ExceptionContext context)
    {
        // Missing or invalid "type" discriminator surfaces as either JsonException
        // or NotSupportedException (abstract type cannot be instantiated) during
        // input formatting — both map to 400 Bad Request.
        if (context.Exception is JsonException or NotSupportedException { Source: "System.Text.Json" })
        {
            context.Result = new BadRequestObjectResult(new { error = context.Exception.Message });
            context.ExceptionHandled = true;
        }
    }
}
