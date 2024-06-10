using Microsoft.AspNetCore.SignalR;

namespace SignalRService.Request
{
    public interface IChatHttpRequest
    {
        string? GetClientGuid();
        bool Exists();
        bool ExistsClientId();
        void SetContext(HubCallerContext context);
    }
}
