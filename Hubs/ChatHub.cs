using Microsoft.AspNetCore.SignalR;
// using Newtonsoft.Json;
using Entities;

namespace SignalRService.Hubs
{
    // [Authorize]
    public class ChatHub : Hub
    {
        private ChatServiceValidation ChatServiceValidation { get; set; }
        private ChatService ChatService { get; set; }
        public ChatHttpRequest HttpRequest { get; }

        private readonly IHubContext<ChatHub> _hubContext;
        private static readonly Dictionary<string, Client> ServiceClients = new();

        /// <summary>
        /// Hub initialization
        /// </summary>
        /// <param name="hubContext">Chat hub context</param>
        public ChatHub(IHubContext<ChatHub> hubContext)
        {
            _hubContext = hubContext;
            ChatServiceValidation = new ChatServiceValidation(Context);
            ChatService = new ChatService(ServiceClients, Context, Clients);
            HttpRequest = new ChatHttpRequest(Context);
            
        }

        #region NEGOTIATE

        public override async Task OnDisconnectedAsync(Exception exception)
        {
            try
            {
                if (!ChatServiceValidation.IsValid()) { 
                    throw new Exception("Unable to validate requests");
                }
                
                await Offline();

                await this.Disconnect(Context.ConnectionId);

                await base.OnDisconnectedAsync(exception);
            }
            catch (System.Exception ex)
            {
                throw;
            }
        }

        public override async Task OnConnectedAsync()
        {
            try
            {
                if (!ChatServiceValidation.IsValid()) { 
                    throw new Exception("Unable to validate requests");
                }

                var clientGuid = HttpRequest.GetClientGuid();

                if (string.IsNullOrEmpty(clientGuid)){
                    throw new Exception("Unknown Client Guid");
                }

                ChatService.AddClient(clientGuid);

                await base.OnConnectedAsync();
            }
            catch (System.Exception ex)
            {
                throw;
            }
        }

        #endregion

        #region "PUBLIC METHODS"

        /// <summary>
        /// Disconnect client from the service.
        /// </summary>
        /// <param name="connectionId">Client unique identifier</param>
        private async Task Disconnect(string connectionId)
        {
            try
            {
                if (!ChatServiceValidation.IsValid()) { 
                    throw new Exception("Unable to validate requests");
                }

                Client? client = ChatService.GetClient() ?? throw new Exception("Client request not exists");

                await ClientClean(client, connectionId);
            }
            catch (System.Exception ex)
            {
                Console.WriteLine(string.Format("ERROR Disconnect: {0} ", ex.Message));
                throw;
            }
        }

        /// <summary>
        /// Hub method to create a group chat.
        /// </summary>
        /// <param name="groupGuid">Group unique identifier</param>
        public async Task AddToGroup(string groupGuid)
        {
            try
            {
                if (!ChatServiceValidation.IsValid()) { 
                    throw new Exception("Unable to validate requests");
                }

                groupGuid = groupGuid.ToUpper();

                await Groups.AddToGroupAsync(Context.ConnectionId, groupGuid);

                ChatService.AddGroup(groupGuid);
            }
            catch (System.Exception ex)
            {
                await OnDisconnectedAsync(ex);
                throw;
            }
        }

        /// <summary>
        /// Hub method to add user to the service if not alredy exists.
        /// </summary>
        /// <param name="userGuid">User unique identifier</param>
        public async Task<string> AddUser(string userGuid)
        {
            try
            {
                if (!ChatServiceValidation.IsValid()) { 
                    throw new Exception("Unable to validate requests");
                }

                userGuid = userGuid.ToUpper();

                Client? client = ChatService.GetClient() ?? throw new Exception("Client request not exists");

                User? item = client.Users.FirstOrDefault(x => x.Id.Equals(userGuid));

                if (item == null)
                {
                    client.AddUser(
                        new Entities.User() { ConnectionId = Context.ConnectionId, Id = userGuid }
                    );
                }
                else
                {
                    item.ConnectionId = Context.ConnectionId;
                }

                User logged = client.GetUser(Context.ConnectionId, true);

                List<User> users = client.GetUserDistinctAll(logged, false);

                string userConnectionId = string.Join(
                    ";",
                    users.Select(u => u.Id).Distinct().ToList()
                );

                return userConnectionId;
            }
            catch (System.Exception ex)
            {
                if (!string.IsNullOrEmpty(Context.ConnectionId))
                {
                    await OnDisconnectedAsync(ex);
                }

                // Console.WriteLine(string.Format("ERROR AddUser: {0} ", ex.Message));
                throw;
            }
        }

        /// <summary>
        /// Refresh service Users and Groups. Deletes itself and groups if users count it's equals zero.
        /// </summary>
        /// <param name="client">Current Client</param>
        /// <param name="connectionId">Currect user connectionId</param>
        /// <returns></returns>
        public async Task ClientClean(Client client, string connectionId)
        {
            try
            {
                User u = client.GetUser(connectionId, true);

                if (u != null)
                {
                    client.DeleteUser(u.Id);
                }

                if (client.Users.Count == 0)
                {
                    foreach (var group in client.GetGroups())
                    {
                        await Groups.RemoveFromGroupAsync(connectionId, group.Id);
                    }

                    client.RemoveGroups();
                }

                if (client.IsEmpty())
                {
                    ServiceClients.Remove(client.Id);
                }
            }
            catch (System.Exception)
            {
                throw;
            }
        }

        /// <summary>
        /// Sends a message to all users in platform
        /// </summary>
        /// <param name="message">Message as string</param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        public async Task SendAll(string message)
        {
            try
            {
                await Clients.All.SendAsync("ReceiveAll", message);
            }
            catch (System.Exception)
            {
                throw;
            }
        }

        /// <summary>
        /// Send a private messaje to another Client
        /// </summary>
        /// <param name="fromUserGuid">User Guid thats sends the message</param>
        /// <param name="toUserGuid">User Guid that will be receive the message</param>
        /// <param name="message">Message sended as string</param>
        /// <returns>Http ClientProxy response</returns>
        public async Task SendPrivateMessage( string fromUserGuid, string toUserGuid, string message, string messageGuid) {
            try
            {

                if (!ChatServiceValidation.IsValid()) { 
                    throw new Exception("Unable to validate requests");
                }

                fromUserGuid = fromUserGuid.ToUpper();
                toUserGuid = toUserGuid.ToUpper();
                messageGuid = messageGuid.ToUpper();
                
                Client? client = ChatService.GetClient() ?? throw new Exception("Client request not exists");
                User user = client.GetUser(toUserGuid, false) ?? throw new Exception($"User with GUID {toUserGuid} not exists");

                await Clients
                    .Client(user.ConnectionId)
                    .SendAsync(
                        "ReceivePrivateMessage",
                        fromUserGuid,
                        message,
                        messageGuid,
                        DateTime.UtcNow.ToString("o")
                    );
            }
            catch (System.Exception)
            {
                throw;
            }
        }

        /// <summary>
        /// Delete client message.
        /// </summary>
        /// <param name="messageGuid">Message unique identifier</param>
        /// <param name="sourceGuid">Represents the Group or destination User unique identifier</param>
        /// <param name="fromGroup">Indicates if the messages comes from chat Group</param>
        /// <returns></returns>
        public async Task DeleteMessage( string messageGuid, string sourceGuid = "", bool fromGroup = false) {
            try
            {
                if (!ChatServiceValidation.IsValid()) { 
                    throw new Exception("Unable to validate requests");
                }
                
                if (fromGroup && !Guid.TryParse(sourceGuid, out Guid groupGuid))
                {
                    throw new Exception("Invalid Group identifier");
                }

                messageGuid = messageGuid.ToUpper();
                sourceGuid = sourceGuid.ToUpper();

                Client? client = ChatService.GetClient() ?? throw new Exception("Client request not exists");

                if (fromGroup)
                {
                    Group? group = client.Groups.FirstOrDefault(w => w.Id.Equals(sourceGuid.ToUpper()));
                        
                    if (object.Equals(group, null))
                    {
                        throw new Exception("Group not exists");
                    }

                    foreach (User user in group.Members.Where(member => !member.ConnectionId.Equals(Context.ConnectionId)))
                    {
                        await Clients
                            .Client(user.ConnectionId)
                            .SendAsync("DeleteMessage", messageGuid, sourceGuid, fromGroup);
                    }
                }
                else
                {
                    User? user = client.Users.Where(u => !u.ConnectionId.Equals(Context.ConnectionId)).FirstOrDefault(w => w.Id.Equals(sourceGuid));

                    // it is possible that the user does not exist if it logged out
                    if (object.Equals(user, null)) { return; }

                    await Clients
                        .Client(user.ConnectionId)
                        .SendAsync("DeleteMessage", messageGuid, sourceGuid, fromGroup);
                }
            }
            catch (System.Exception)
            {
                throw;
            }
        }

        /// <summary>
        /// Indicates to all connected users, that the user it's Online.
        /// </summary>
        public async Task Online()
        {
            try
            {
                if (!ChatServiceValidation.IsValid()) { 
                    throw new Exception("Unable to validate requests");
                }

                Client? client = ChatService.GetClient() ?? throw new Exception("Client request not exists");

                User logged = client.GetUser(Context.ConnectionId, true);

                List<User> users = client.GetUserDistinctFromGroup();

                foreach (
                    string userConnectionId in users.Where(u => !u.ConnectionId.Equals(Context.ConnectionId)).Select(u => u.ConnectionId).Distinct().ToList()
                )
                {
                    try
                    {
                        User toUser = client.GetUser(userConnectionId, true);

                        await Clients
                            .Client(toUser.ConnectionId)
                            .SendAsync("UserConnected", logged.Id.ToUpper());
                    }
                    catch (System.Exception)
                    {
                        continue;
                    }
                }
            }
            catch (System.Exception)
            {
                throw;
            }
        }

        /// <summary>
        /// Indicates to all connected users, that the user it's Offline.
        /// </summary>
        public async Task Offline() {
            try
            {
                if (!ChatServiceValidation.IsValid()) { 
                    throw new Exception("Unable to validate requests");
                }

                Client? client = ChatService.GetClient() ?? throw new Exception("Client request not exists");

                User logged = client.GetUser(Context.ConnectionId, true);

                List<User> users = client.GetUserDistinctFromGroup();

                foreach (
                    string userConnectionId in users.Where(u => !u.ConnectionId.Equals(Context.ConnectionId)).Select(u => u.ConnectionId).Distinct().ToList()
                )
                {
                    try
                    {
                        User toUser = client.GetUser(userConnectionId, true);

                        await Clients
                            .Client(toUser.ConnectionId)
                            .SendAsync("UserDisconnected", logged.Id.ToUpper());
                    }
                    catch (System.Exception)
                    {
                        continue;
                    }
                }
            }
            catch (System.Exception)
            {
                throw;
            }
        }

        /// <summary>
        /// Sends a message to a group.
        /// </summary>
        /// <param name="user">User that sends the message. Currently used as Guid</param>
        /// <param name="message">User message as string</param>
        /// <param name="groupName">Destination Group name. Currently used as Guid</param>
        /// <returns>Http ClientProxy response</returns>
        /// <exception cref="Exception"></exception>
        public async Task SendGroupMessage(string fromUserGuid, string toGroupGuid, string message, string messageGuid)
        {
            try
            {
                fromUserGuid = fromUserGuid.ToUpper();
                toGroupGuid = toGroupGuid.ToUpper();
                messageGuid = messageGuid.ToUpper();

                await Clients
                    .Group(toGroupGuid)
                    .SendAsync(
                        "ReceiveGroupMessage",
                        fromUserGuid,
                        toGroupGuid,
                        message,
                        messageGuid,
                        DateTime.UtcNow.ToString("o")
                    );
            }
            catch (System.Exception ex)
            {
                throw;
            }
        }
    

        #endregion

    }
}
