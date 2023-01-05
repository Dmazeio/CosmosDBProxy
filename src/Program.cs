using MyProxy;
using System.Text;
using System.Text.Json;
using Yarp.ReverseProxy.Configuration;
using Yarp.ReverseProxy.Forwarder;
using Yarp.ReverseProxy.Transforms;
using Yarp.ReverseProxy.Transforms.Builder;

// hacky!
State.CosmosIpAddress = Environment.GetEnvironmentVariable("CosmosIp") ?? "192.168.80.2";

var routes = new[]
    {
        new RouteConfig()
        {
            RouteId = "route1",
            ClusterId = "cluster1",
            Match = new RouteMatch
            {
                Path = "{**catch-all}"
            },
        }
    };
var clusters = new[]
{
        new ClusterConfig()
        {
            ClusterId = "cluster1",
            Destinations = new Dictionary<string, DestinationConfig>(StringComparer.OrdinalIgnoreCase)
            {
                { "destination1", new DestinationConfig() { Address = $"https://{State.CosmosIpAddress}:8081" } }
            },
            HttpClient = new HttpClientConfig
            {
                DangerousAcceptAnyServerCertificate= true,
            },
            HttpRequest = new ForwarderRequestConfig
            {
                ActivityTimeout = TimeSpan.FromMinutes(5)
            }

        }
    };

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddReverseProxy()
    .LoadFromMemory(routes, clusters)
    .AddTransforms<MyTransform>();

var app = builder.Build();
app.Services.GetService<ILogger<Program>>()?.LogInformation("Cosmos IP: {cosmosIp}", State.CosmosIpAddress);

app.MapReverseProxy();
app.Run();

internal class MyTransform : ITransformProvider
{
    private readonly ILogger<MyTransform> _logger;

    public MyTransform(ILogger<MyTransform> logger)
    {
        _logger = logger;
    }
    public void Apply(TransformBuilderContext context)
    {
        context.AddRequestTransform(requestContext =>
        {
            _logger.LogInformation("Request URI: {dest}{path}, Headers: {headers}", requestContext.DestinationPrefix, requestContext.Path, Stringify(requestContext));
            return ValueTask.CompletedTask;
        });
        context.AddResponseTransform(async responseContext =>
        {
            if (responseContext.ProxyResponse == null) return;

            _logger.LogInformation("Response came back, Status: {status}", responseContext.ProxyResponse.StatusCode);

            if (responseContext.ProxyResponse.RequestMessage?.RequestUri?.AbsolutePath != "/")
            {
                return;
            }

            var stream = await responseContext.ProxyResponse.Content.ReadAsStreamAsync();
            using var reader = new StreamReader(stream);
            var body = await reader.ReadToEndAsync();

            if (!string.IsNullOrEmpty(body))
            {
                var cosmosResponse = JsonSerializer.Deserialize<CosmosResponse>(body);
                if (cosmosResponse == null)
                {
                    _logger.LogError("Failed to deserialize response, it was: {responseBody}", body);
                    return;
                }

                _logger.LogInformation("Setting return URLs to: {hostUrl}", responseContext.HttpContext.Request.Host.ToString());
                cosmosResponse._rid = responseContext.HttpContext.Request.Host.ToString();
                foreach (var item in cosmosResponse.writableLocations)
                {
                    item.databaseAccountEndpoint = $"http://{responseContext.HttpContext.Request.Host}/";
                }

                foreach (var item in cosmosResponse.readableLocations)
                {
                    item.databaseAccountEndpoint = $"http://{responseContext.HttpContext.Request.Host}/";
                }

                responseContext.SuppressResponseBody = true;

                body = JsonSerializer.Serialize(cosmosResponse);
                _logger.LogInformation("Body: {body}", body);
                var bytes = Encoding.UTF8.GetBytes(body);
                // Change Content-Length to match the modified body, or remove it.
                responseContext.HttpContext.Response.ContentLength = bytes.Length;
                // Response headers are copied before transforms are invoked, update any needed headers on the HttpContext.Response.
                await responseContext.HttpContext.Response.Body.WriteAsync(bytes);
            }
        });
    }

    private static string Stringify(RequestTransformContext requestContext)
    {
        var headerStrings = requestContext.ProxyRequest.Headers
            .Select(h => new { key = h.Key, value = string.Join(",", h.Value) })
            .Select(a => $"{a.key}:{a.value}");
        return string.Join(" - ", headerStrings);
    }

    public void ValidateCluster(TransformClusterValidationContext context)
    {
    }

    public void ValidateRoute(TransformRouteValidationContext context)
    {
        _logger.LogInformation("Route: {route}", context.Route);
    }
}
