using System.Net;

public class FirstMiddleware
{
    private readonly RequestDelegate _next;
    private const string ContextApiKey = "X-APIKEY";

    public FirstMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    private readonly Dictionary<Guid, string[]> _AllowedAppsHubs = new()
    {
        { new Guid("8E2361D4-2E57-43A3-A809-E67CB5A8D0EA"), new string[] { "/chatHub/negotiate" } },
        { new Guid("34B57314-AEA4-4298-9935-199010D97DF1"), new string[] { "/chatHub/negotiate" } },
        { new Guid("F61BB955-7D91-4A5D-BCE2-A73EF178E68F"), new string[] { "/chatHub/negotiate" } },
        { new Guid("3145D144-2CE2-49A9-AC53-DC3D6A82080B"), new string[] { "/chatHub/negotiate" } },
        { new Guid("12C93A6E-7F2D-48A1-8499-EF487500D933"), new string[] { "/chatHub/negotiate" } },
        { new Guid("4DF8B99E-3352-4E1B-B1C1-2F67CA4F6A88"), new string[] { "/chatHub/negotiate" } },
        { new Guid("481B6B78-1024-4286-B75B-3AED4A39FF0A"), new string[] { "/chatHub/negotiate" } }
    };

    private readonly string[] _AllowedHubs = new string[] { "/chatHub" };

    private List<string> GetAllowdEndpoints()
    {
        return _AllowedAppsHubs.Values.SelectMany(v => v).Distinct().ToList();
    }

    public async Task InvokeAsync(HttpContext context)
    {
        string path = string.Empty;

        try
        {
            if (context.Request != null && !context.Request.Path.HasValue)
            {
                await _next(context);
                return;
            }

            if (context.Request != null && context.Request.Path.HasValue)
            {
                path = context.Request.Path.Value;

                if (!IsRoot(path))
                {
                    HubRequestManage(context);
                }
            }

            await _next(context);
        }
        catch (System.Exception ex)
        {
            // throw new Exception("You are not allowed to perform any request at this server");
            throw new Exception("ERROR: " + ex.Message);
        }
    }

    private bool HubRequestManage(HttpContext context)
    {
        try
        {
            var host = context.Request.Host;
            // var headers = context.Request.Headers;
            // var routeValues = context.Request.RouteValues;
            string path = context.Request.Path.Value ?? string.Empty;
            var appKey = context.Request.Headers["x-app-id"].ToString();
            var apiKey = context.Request.Headers["x-apikey"].ToString();

            if (IsAllowedHub(path.Split('/')[0].ToString()))
            {
                if (
                    !string.IsNullOrEmpty(appKey)
                    && (!IsAllowedAppHub(path) && !IsAllowedApp(new Guid(appKey)))
                )
                {
                    context.Response.StatusCode = (int)HttpStatusCode.Unauthorized;
                    var message = "Unauthorized: se requiere autenticación.";
                    throw new UnauthorizedAccessException(message);

                    // throw new Exception(
                    //     string.Format(
                    //         "tests not passed: APPKEY {0}, IsAllowedAppHub: {1}, IsAllowedApp: {2}, Path: {3}",
                    //         appKey,
                    //         IsAllowedAppHub(path),
                    //         IsAllowedApp(new Guid(appKey)),
                    //         path
                    //     )
                    // );
                }
            }

            // var appRoute = routeValues.Keys.Where(w => w.Equals("app")).FirstOrDefault();
            // return (exists.Key != null && exists.Value != null);

            return true;
        }
        catch (System.Exception)
        {
            throw;
        }
    }

    private bool IsRoot(string path)
    {
        return path.Equals("/");
    }

    private bool IsAllowedHub(string path)
    {
        return _AllowedHubs.Where(w => w.Contains(path)).Any();
    }

    private bool IsAllowedAppHub(string path)
    {
        return GetAllowdEndpoints().Where(w => w.Equals(path)).Any();
    }

    private bool IsAllowedApp(Guid AppKey)
    {
        return _AllowedAppsHubs.Where(m => m.Key == AppKey).ToList().Any();
    }
}
