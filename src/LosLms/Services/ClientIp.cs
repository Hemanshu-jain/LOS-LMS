using System.Net;

namespace LosLms.Services;

/// <summary>The caller's address for rate limits and audit lines.</summary>
public static class ClientIp
{
    /// <summary>
    /// CF-Connecting-IP only counts when the TCP peer is loopback, i.e. the request really came through the local
    /// Cloudflare tunnel; from anywhere else the header is just something the caller typed, so the socket address wins.
    /// (Program.cs does not apply X-Forwarded-For, so RemoteIpAddress is the real peer.)
    /// </summary>
    public static string Of(HttpContext context)
    {
        var socket = context.Connection.RemoteIpAddress;
        if (socket is not null && IPAddress.IsLoopback(socket)
            && IPAddress.TryParse(context.Request.Headers["CF-Connecting-IP"].FirstOrDefault(), out var forwarded))
        {
            return forwarded.ToString();
        }

        return socket?.ToString() ?? "unknown";
    }
}
