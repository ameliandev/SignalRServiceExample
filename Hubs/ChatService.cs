using Microsoft.AspNetCore.SignalR;
using Entities;

namespace SignalRService.Hubs
{
    public class ChatService
    {
        private ChatServiceValidation ChatServiceValidation { get; set; }
        private Dictionary<string, Entities.Client> ServiceClients { get; set; }
        private HubCallerContext Context { get; set; }

        private IHubCallerClients Clients { get; set; }

        public ChatService(Dictionary<string, Client> serviceClients, HubCallerContext context, IHubCallerClients clients)
        { 
            if (serviceClients == null || context == null) { 
                throw new Exception("Unable to validate requests");
            }

            this.ServiceClients = serviceClients;
            this.Context = context;
            this.Clients = clients;

            ChatServiceValidation = new ChatServiceValidation(serviceClients,Context);
        }

        /// <summary>
        /// Creates a new group
        /// </summary>
        /// <param name="groupGuid">Group unique identifier</param>
        /// <exception cref="Exception"></exception>
        public void AddGroup(string groupGuid)
        {
            try
            {
                Client client = ChatServiceValidation.ValidateRequest() ?? throw new Exception("No Client found");
                User user = client.GetUser(Context.ConnectionId, true);
                Group group = client.Groups.FirstOrDefault(x => x.Id.Equals(groupGuid));

                if (group == null)
                {
                    Group newGroup = new()
                    {
                        Id = groupGuid,
                        Members = new List<User>()
                    };

                    newGroup.Members.Add(
                        new User() { ConnectionId = Context.ConnectionId, Id = user.Id }
                    );

                    client.Groups.Add(newGroup);
                }
                else
                {
                    group.Members.Add(
                        new User() { ConnectionId = Context.ConnectionId, Id = user.Id }
                    );
                }
            }
            catch (System.Exception)
            {
                if (!string.IsNullOrEmpty(Context.ConnectionId))
                {
                    throw new Exception("Invalid connection id");
                }

                throw;
            }
        }

        /// <summary>
        /// Add the client to the user cache dictionary
        /// </summary>
        /// <param name="clientGuid">Client's unique identifier</param>
        public void AddClient(string clientGuid)
        {
            try
            {
                clientGuid = clientGuid.ToUpper();

                if (ServiceClients.ContainsKey(clientGuid)) { return; }

                Client newClient = new()
                {
                    Id = clientGuid,
                    Users = new List<User>(),
                    Groups = new List<Group>()
                };

                ServiceClients.Add(clientGuid, newClient);
            }
            catch (System.Exception)
            {
                throw;
            }
        }
        
    }
}