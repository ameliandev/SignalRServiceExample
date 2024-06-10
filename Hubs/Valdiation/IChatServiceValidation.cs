using Microsoft.AspNetCore.SignalR;

namespace SignalRService.Validation
{
    public interface IChatServiceValidation
    {
        bool IsValid();

        void SetContext(HubCallerContext context);
    }
}
