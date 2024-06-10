using Microsoft.AspNetCore.SignalR;
using Entities;
using SignalRService.Validation;
using SignalRService.Request;
using SignalRService.Service;

namespace SignalRService.Hubs
{
    public class ChatService : IChatService
    {
        private Dictionary<string, Entities.Client>? ServiceClients { get; set; }
        private readonly IChatServiceValidation _chatServiceValidation;
        private readonly IChatHttpRequest _chatHttpRequest;
        private HubCallerContext? Context { get; set; }

        public ChatService(IChatServiceValidation chatServiceValidation, ChatHttpRequest chatHttpRequest)
        {
            _chatServiceValidation = chatServiceValidation;
            _chatHttpRequest = chatHttpRequest;
        }

        /// <summary>
        /// Creates a new group
        /// </summary>
        /// <param name="groupGuid">Group unique identifier</param>
        /// <exception cref="Exception"></exception>
        public void AddGroup(string groupGuid)
        {

            if (object.Equals(_chatServiceValidation, null) || !_chatServiceValidation.IsValid())
            {
                throw new Exception("Invalid service request");
            }

            if (object.Equals(Context, null))
            {
                throw new Exception("Null context detected.");
            }

            try
            {

                Client? client = this.GetClient() ?? throw new Exception("Client request not exists");
                User? user = client.GetUser(Context.ConnectionId, true);
                Group? group = client.GetGroup(groupGuid);

                if (object.Equals(user, null))
                {
                    throw new Exception($"User with connection id ${Context.ConnectionId} not found.");
                }

                if (group == null)
                {
                    Group newGroup = new()
                    {
                        Id = groupGuid,
                        Members = new List<User>() { new() {
                            ConnectionId = Context.ConnectionId,
                            Id = user.Id
                        }}
                    };

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

            if (object.Equals(ServiceClients, null))
            {
                throw new Exception("Null clients detected");
            }

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

        /// <summary>
        /// Get the Client
        /// </summary>
        /// <returns>Return a Client object if it exists</returns>
        public Client? GetClient()
        {
            if (object.Equals(ServiceClients, null))
            {
                throw new Exception("Null clients detected");
            }

            if (object.Equals(_chatHttpRequest, null))
            {
                throw new Exception("Null Request detected");
            }

            try
            {

                string clientGuid = _chatHttpRequest.GetClientGuid() ?? string.Empty;

                if (!ServiceClients.ContainsKey(clientGuid))
                {
                    return null;
                }

                return this.ServiceClients[clientGuid];
            }
            catch (System.Exception)
            {
                throw;
            }
        }

        public void SetClient(Dictionary<string, Client> serviceClients)
        {
            ServiceClients = serviceClients;
        }

        public void SetContext(HubCallerContext context)
        {
            Context = context;
            _chatServiceValidation.SetContext(context);
            _chatHttpRequest.SetContext(context);
        }
    }
}