using Microsoft.AspNetCore.SignalR;

public class CustomHubFilter : IHubFilter
{
    public async Task<object?> InvokeMethodAsync(
        HubInvocationContext invocationContext,
        Func<HubInvocationContext, Task<object?>> next
    )
    {
        var hubType = invocationContext.Context.GetType();
        var hubMethodName = invocationContext.HubMethod.Name;

        if (hubType == typeof(SignalRService.Hubs.ChatHub) && hubMethodName == "MethodName")
        {
            if (!IsUserAuthorized(invocationContext))
            {
                throw new UnauthorizedAccessException(
                    "You do not have the necessary permissions to access this method."
                );
            }
        }

        return await next(invocationContext);
    }

    private bool IsUserAuthorized(HubInvocationContext invocationContext)
    {
        return true;
    }
}
