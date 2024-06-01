using Microsoft.AspNetCore.SignalR;
using Entities;

namespace SignalRService.Hubs {

    public class ChatServiceValidation
    {
        private HubCallerContext Context { get; set; }
        private ChatHttpRequest HttpRequest { get; set; }

        /// <summary>
        /// Create a new service validation to prevent incorrect or not valid requests.
        /// </summary>
        /// <param name="serviceClients">The main instance of the service connected clients</param>
        /// <param name="context">Context of the hub caller connection</param>
        /// <exception cref="Exception"></exception>
        public ChatServiceValidation(HubCallerContext context)
        {
            HttpRequest = new ChatHttpRequest(context);
            Context = context;
        }

        /// <summary>
        /// Validate each client hub request
        /// </summary>
        /// <param name="firstCall">First call it's possible because the service hub negotiate.</param>
        /// <returns>If the validation was success, return the new client context.</returns>
        /// <exception cref="Exception"></exception>
        public bool IsValid()
        {

            if (!HttpRequest.Exists()) {
                return false;
            }

            if (!ExistsConnectionId()) { 
                return false;
            }

            if (!HttpRequest.ExistsClientId()) { 
                return false;
            }

            // if (firstCall)
            // {
            //     return null;
            // }

            return true;
        }

        /// <summary>
        /// Validates if the context connection id exists. It's required.
        /// </summary>
        /// <exception cref="Exception"></exception>
        public bool ExistsConnectionId()
        {
            try
            {
                return this.Context != null && !string.IsNullOrEmpty(this.Context.ConnectionId);
            }
            catch (System.Exception)
            {
                return false;
            }
        }
    }
}
