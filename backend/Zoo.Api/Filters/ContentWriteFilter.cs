using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.EntityFrameworkCore;
using Zoo.DAL.Data;
namespace Zoo.Api.Filters;

public sealed class ContentWriteFilter(ZooDbContext db) : IAsyncActionFilter
{
    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        if (HttpMethods.IsGet(context.HttpContext.Request.Method)) { await next(); return; }
        await db.Database.OpenConnectionAsync(context.HttpContext.RequestAborted);
        try
        {
            await using var command = db.Database.GetDbConnection().CreateCommand();
            command.CommandText = "DECLARE @r int; EXEC @r = sp_getapplock @Resource='Zoo.Content.Write', @LockMode='Exclusive', @LockOwner='Session', @LockTimeout=0; SELECT @r;";
            var result = Convert.ToInt32(await command.ExecuteScalarAsync(context.HttpContext.RequestAborted));
            if (result < 0)
            {
                context.Result = new ConflictObjectResult(new { message = "Nội dung đang được xử lý. Vui lòng thử lại sau." });
                return;
            }
            try { await next(); }
            finally
            {
                command.CommandText = "EXEC sp_releaseapplock @Resource='Zoo.Content.Write', @LockOwner='Session';";
                await command.ExecuteNonQueryAsync(CancellationToken.None);
            }
        }
        finally { await db.Database.CloseConnectionAsync(); }
    }
}
