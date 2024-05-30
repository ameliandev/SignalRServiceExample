using Microsoft.AspNetCore.SignalR;
using Entities;

namespace SignalRService.Hubs {

    public class ChatServiceValidation
    {
        //Context.ConnectionId hace referencia al Id único que tienen los usuarios al conectarse.

        private const string ClientToken = "clientId";
        private Dictionary<string, Entities.Client> ServiceClients { get; set; }
        private HubCallerContext Context { get; set; }

        /// <summary>
        /// Create a new service validation to prevent incorrect or not valid requests.
        /// </summary>
        /// <param name="serviceClients">The main instance of the service connected clients</param>
        /// <param name="context">Context of the hub caller connection</param>
        /// <exception cref="Exception"></exception>
        public ChatServiceValidation(Dictionary<string, Entities.Client> serviceClients, HubCallerContext context)
        {
            if (serviceClients == null || context == null) { 
                throw new Exception("Unable to validate requests");
            }

            this.ServiceClients = serviceClients;
            this.Context = context;
        }

        /// <summary>
        /// Validate each client hub request
        /// </summary>
        /// <param name="firstCall">First call it's possible because the service hub negotiate.</param>
        /// <returns>If the validation was success, return the new client context.</returns>
        /// <exception cref="Exception"></exception>
        public Client? ValidateRequest(bool firstCall = false)
        {
            RequestIsEmpty();

            ValidateConnectionId();

            string clientGuid = GetClientGuid();

            if (string.IsNullOrEmpty(clientGuid))
            {
                throw new Exception("Invalid client");
            }

            if (firstCall)
            {
                return null;
            }

            if (!this.ServiceClients.ContainsKey(clientGuid))
            {
                throw new Exception("Client not exists");
            }

            return this.ServiceClients[clientGuid];
        }

        /// <summary>
        /// If the current request it's empty, throw an exception because it's required for any request.
        /// </summary>
        private void RequestIsEmpty()
        {
            try
            {
                if ((this.Context?.GetHttpContext()?.Request == null))
                {
                    throw new Exception("Request it's empty");
                }
            }
            catch (System.Exception)
            {
                throw;
            }
        }

        /// <summary>
        /// Get's the current Http client request
        /// </summary>
        /// <returns></returns>
        internal HttpRequest? GetHttpRequest()
        {
            return this.Context?.GetHttpContext()?.Request;
        }

        /// <summary>
        /// Gets the current Client unique identifier.
        /// </summary>
        /// <returns>A string as Guid from current context Client</returns>
        internal string GetClientGuid()
        {
            try
            {
                return GetHttpRequest().RouteValues?[ClientToken].ToString().ToUpper();
            }
            catch (System.Exception)
            {
                return string.Empty;
            }
        }

        /// <summary>
        /// Valdiates if the context connection id exists. It's required.
        /// </summary>
        /// <exception cref="Exception"></exception>
        public void ValidateConnectionId()
        {
            try
            {
                if (this.Context != null && string.IsNullOrEmpty(this.Context.ConnectionId))
                {
                    throw new Exception("ConnectionId is not valid");
                }
            }
            catch (System.Exception ex)
            {
                throw new Exception("ERROR ValidateConnectionId: " + ex.Message);
            }
        }
    }
}