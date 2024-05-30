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
        private readonly IHubContext<ChatHub> _hubContext;
        private static readonly Dictionary<string, Client> ServiceClients = new();

        /// <summary>
        /// Hub initialization
        /// </summary>
        /// <param name="hubContext">Chat hub context</param>
        public ChatHub(IHubContext<ChatHub> hubContext)
        {
            _hubContext = hubContext;
            ChatServiceValidation = new ChatServiceValidation(ServiceClients,Context);
            ChatService = new ChatService(ServiceClients, Context, Clients);
        }

        #region NEGOTIATE

        private async Task Disconnect(string connectionId)
        {
            try
            {
                Entities.Client client = ChatServiceValidation.ValidateRequest() ?? throw new Exception("Client not found");
                await ClientClean(client, connectionId);
            }
            catch (System.Exception ex)
            {
                Console.WriteLine(string.Format("ERROR Disconnect: {0} ", ex.Message));
                throw;
            }
        }

        public override async Task OnDisconnectedAsync(Exception exception)
        {
            try
            {
                Entities.Client client = ChatServiceValidation.ValidateRequest() ?? throw new Exception("Client not found");

                await Offline();

                await this.Disconnect(Context.ConnectionId);

                await base.OnDisconnectedAsync(exception);
            }
            catch (System.Exception ex)
            {
                Console.WriteLine(string.Format("ERROR OnDisconnectedAsync: {0} ", ex.Message));
                throw;
            }
        }

        public override async Task OnConnectedAsync()
        {
            try
            {
                ChatServiceValidation.ValidateRequest(true);

                var clientGuid = ChatServiceValidation.GetClientGuid();

                if (!string.IsNullOrEmpty(clientGuid))
                {
                    ChatService.AddClient(clientGuid);
                }
                else
                {
                    throw new Exception("Unknown Client Guid");
                }

                await base.OnConnectedAsync();
            }
            catch (System.Exception ex)
            {
                Console.WriteLine(string.Format("ERROR OnConnectedAsync: {0} ", ex.Message));
                throw;
            }
        }

        #endregion

        #region PUBLIC METHODS

        public async Task AddToGroup(string groupGuid)
        {
            try
            {
                groupGuid = groupGuid.ToUpper();

                ChatServiceValidation.ValidateRequest();

                await Groups.AddToGroupAsync(Context.ConnectionId, groupGuid);

                ChatService.AddGroup(groupGuid);
            }
            catch (System.Exception ex)
            {
                await OnDisconnectedAsync(ex);
                throw;
            }
        }

        public async Task<string> AddUser(string userGuid)
        {
            try
            {
                userGuid = userGuid.ToUpper();

                Entities.Client client = ChatServiceValidation.ValidateRequest();

                Entities.User item = client.Users.Where(x => x.Id.Equals(userGuid)).FirstOrDefault();

                if (item == null)
                {
                    client.AddUser(
                        new Entities.User() { ConnectionId = Context.ConnectionId, Id = userGuid }
                    );

                    Console.WriteLine($"User {userGuid} - {Context.ConnectionId} created");
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

                Console.WriteLine(string.Format("ERROR AddUser: {0} ", ex.Message));
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
                Entities.User u = client.GetUser(connectionId, true);

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
            catch (System.Exception ex)
            {
                Console.WriteLine(string.Format("ERROR ClientClean: {0} ", ex.Message));
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
            catch (System.Exception ex)
            {
                Console.WriteLine(string.Format("ERROR SendAll: {0} ", ex.Message));
                throw;
            }
        }

        /// <summary>
        /// Send a private messaje to Client using the destination ConnectionID
        /// </summary>
        /// <param name="fromUserGuid">User Guid thats sends the message</param>
        /// <param name="toUserGuid">User Guid that will be receive the message</param>
        /// <param name="message">Message sended as string</param>
        /// <returns>Http ClientProxy response</returns>
        public async Task SendPrivateMessage(
            string fromUserGuid,
            string toUserGuid,
            string message,
            string messageGuid
        )
        {
            try
            {
                fromUserGuid = fromUserGuid.ToUpper();
                toUserGuid = toUserGuid.ToUpper();
                messageGuid = messageGuid.ToUpper();

                Client client = ChatServiceValidation.ValidateRequest() ?? throw new Exception("Client not found");

                Entities.User user = client.GetUser(toUserGuid, false) ?? throw new Exception($"User with GUID {toUserGuid} not exists");

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
            catch (System.Exception ex)
            {
                Console.WriteLine(string.Format("ERROR SendPrivateMessage: {0} ", ex.Message));
                throw;
            }
        }

        /// <summary>
        /// Indicates if one messages was deleted from chat interface by user.
        /// </summary>
        /// <param name="messageGuid">Message unique identifier</param>
        /// <param name="sourceGuid">Represents the Group or destination User unique identifier</param>
        /// <param name="fromGroup">Indicates if the messages comes from chat Group</param>
        /// <returns></returns>
        public async Task DeleteMessage(
            string messageGuid,
            string? sourceGuid,
            bool fromGroup = false
        )
        {
            try
            {
                Client client = ChatServiceValidation.ValidateRequest() ?? throw new Exception("Client not found");;

                messageGuid = messageGuid.ToUpper();

                if (fromGroup && !Guid.TryParse(sourceGuid, out Guid groupGuid))
                {
                    throw new Exception("Group identifier not valid");
                }

                sourceGuid = sourceGuid.ToUpper();

                if (fromGroup)
                {
                    Group g = client.Groups.FirstOrDefault(w => w.Id.Equals(sourceGuid.ToUpper()));
                        
                    if (object.Equals(g, null))
                    {
                        throw new Exception("Group not exists");
                    }

                    foreach (User user in g.Members)
                    {
                        if (user.ConnectionId.Equals(Context.ConnectionId))
                        {
                            // Do nothing if is the same user.
                            continue;
                        }

                        await Clients
                            .Client(user.ConnectionId)
                            .SendAsync("DeleteMessage", messageGuid, sourceGuid, fromGroup);
                    }
                }
                else
                {
                    User u = client.Users.FirstOrDefault(w => w.Id.Equals(sourceGuid));

                    if (
                        object.Equals(u, null)
                        || (!object.Equals(u, null)) && u.ConnectionId.Equals(Context.ConnectionId)
                    )
                    {
                        // Do nothing if is the same user.
                        return;
                    }

                    await Clients
                        .Client(u.ConnectionId)
                        .SendAsync("DeleteMessage", messageGuid, sourceGuid, fromGroup);
                }
            }
            catch (System.Exception)
            {
                throw;
            }
        }

        // public async Task<string> Diagnostic()
        // {
        //     ChatServiceValidation.ValidateRequest();

        //     string text = string.Empty;

        //     string jsonString = JsonConvert.SerializeObject(ServiceClients);

        //     return jsonString;
        // }

        public async Task Online()
        {
            try
            {
                Client client = ChatServiceValidation.ValidateRequest() ?? throw new Exception("Client not found");;

                User logged = client.GetUser(Context.ConnectionId, true);

                List<User> users = client.GetUserDistinctFromGroup();

                foreach (
                    string userConnectionId in users.Select(u => u.ConnectionId).Distinct().ToList()
                )
                {
                    if (userConnectionId.Equals(Context.ConnectionId))
                    {
                        // Do nothing if is the same user.
                        continue;
                    }

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

        public async Task Offline()
        {
            try
            {
                Client client = ChatServiceValidation.ValidateRequest() ?? throw new Exception("Client not found");;

                User logged = client.GetUser(Context.ConnectionId, true);

                List<User> users = client.GetUserDistinctFromGroup();

                foreach (
                    string userConnectionId in users.Select(u => u.ConnectionId).Distinct().ToList()
                )
                {
                    if (userConnectionId.Equals(Context.ConnectionId))
                    {
                        // Do nothing if is the same user.
                        continue;
                    }

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
        /// Sends a message to group.
        /// </summary>
        /// <param name="user">User that sends the message. Currently used as Guid</param>
        /// <param name="message">User message as string</param>
        /// <param name="groupName">Destination Group name. Currently used as Guid</param>
        /// <returns>Http ClientProxy response</returns>
        /// <exception cref="Exception"></exception>
        public async Task SendGroupMessage(
            string fromUserGuid,
            string toGroupGuid,
            string message,
            string messageGuid
        )
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
                Console.WriteLine(string.Format("ERROR SendMessage: {0} ", ex.Message));
                throw;
            }
        }
    }

    #endregion
}
