using Microsoft.AspNetCore.SignalR;

namespace SignalRService.Hubs {

    public class ChatHttpRequest
    {
        private const string ClientToken = "clientId";

        private HubCallerContext Context { get; set; }

        public ChatHttpRequest(HubCallerContext context)
        {
            this.Context = context ?? throw new ArgumentNullException("context required.");
        }

        /// <summary>
        /// Get's the current Http client request
        /// </summary>
        /// <returns>HttpRequest</returns>
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
        /// Check if the current http request exists.
        /// </summary>
        internal bool Exists()
        {
            try
            {
                return this.Context?.GetHttpContext()?.Request != null;
            }
            catch (System.Exception)
            {
                return false;
            }
        }

        /// <summary>
        /// Check if the client id Token exists on the current request.
        /// </summary>
        internal bool ExistsClientId()
        { 
            string clientGuid = this.GetClientGuid();

            return !string.IsNullOrEmpty(clientGuid);
        }

    }

    
}
