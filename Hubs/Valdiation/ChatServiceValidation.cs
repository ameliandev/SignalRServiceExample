using Microsoft.AspNetCore.SignalR;
using SignalRService.Request;

namespace SignalRService.Validation
{

    public class ChatServiceValidation : IChatServiceValidation
    {
        private HubCallerContext? Context { get; set; }
        private readonly IChatHttpRequest _chatHttpRequest;

        public ChatServiceValidation(IChatHttpRequest chatHttpRequest)
        {
            _chatHttpRequest = chatHttpRequest;
        }

        /// <summary>
        /// Validate each client hub request
        /// </summary>
        /// <param name="firstCall">First call it's possible because the service hub negotiate.</param>
        /// <returns>If the validation was success, return the new client context.</returns>
        /// <exception cref="Exception"></exception>
        public bool IsValid()
        {

            if (object.Equals(_chatHttpRequest, null))
            {
                return false;
            };

            if (!_chatHttpRequest.Exists())
            {
                return false;
            }

            if (!ExistsConnectionId())
            {
                return false;
            }

            if (!_chatHttpRequest.ExistsClientId())
            {
                return false;
            }

            return true;
        }

        public void SetContext(HubCallerContext context)
        {
            Context = context;
            _chatHttpRequest.SetContext(context);
        }

        /// <summary>
        /// Validates if the context connection id exists. It's required.
        /// </summary>
        /// <exception cref="Exception"></exception>
        private bool ExistsConnectionId()
        {
            try
            {
                return Context != null && !string.IsNullOrEmpty(Context.ConnectionId);
            }
            catch (System.Exception)
            {
                return false;
            }
        }
    }
}