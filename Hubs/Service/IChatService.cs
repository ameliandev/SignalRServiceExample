using Entities;
using Microsoft.AspNetCore.SignalR;

namespace SignalRService.Service
{
    public interface IChatService
    {
        void AddGroup(string groupGuid);
        void AddClient(string clientGuid);
        Client? GetClient();
        void SetClient(Dictionary<string, Client> serviceClients);
        void SetContext(HubCallerContext context);
    }
}
