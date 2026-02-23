using BackTest.Repositories;

namespace BackTest.Middleware;

public class TenantResolutionMiddleware
{
    private readonly RequestDelegate _next;

    public TenantResolutionMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context, AssetManagementDbContext dbContext)
    {
        var tenantIdHeader = context.Request.Headers["X-Tenant-Id"].FirstOrDefault();

        if (!string.IsNullOrEmpty(tenantIdHeader) && Guid.TryParse(tenantIdHeader, out var tenantId))
        {
            dbContext.SetTenant(tenantId);
            context.Items["TenantId"] = tenantId;
        }

        await _next(context);
    }
}
