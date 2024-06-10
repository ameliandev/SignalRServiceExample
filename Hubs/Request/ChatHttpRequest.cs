using Microsoft.AspNetCore.SignalR;

namespace SignalRService.Request
{

    public class ChatHttpRequest : IChatHttpRequest
    {
        private const string ClientToken = "clientId";

        private HubCallerContext? Context { get; set; }

        /// <summary>
        /// Get's the current Http client request
        /// </summary>
        /// <returns>HttpRequest</returns>
        private HttpRequest? GetHttpRequest()
        {
            return this.Context?.GetHttpContext()?.Request;
        }

        /// <summary>
        /// Gets the current Client unique identifier.
        /// </summary>
        /// <returns>A string as Guid from current context Client</returns>
        public string? GetClientGuid()
        {
            try
            {
                return GetHttpRequest()?.RouteValues?[ClientToken]?.ToString()?.ToUpper();
            }
            catch (System.Exception)
            {
                return string.Empty;
            }
        }

        /// <summary>
        /// Check if the current http request exists.
        /// </summary>
        public bool Exists()
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
        public bool ExistsClientId()
        {
            string clientGuid = GetClientGuid() ?? string.Empty;

            return !string.IsNullOrEmpty(clientGuid);
        }

        public void SetContext(HubCallerContext context)
        {
            Context = context;
        }

    }


}