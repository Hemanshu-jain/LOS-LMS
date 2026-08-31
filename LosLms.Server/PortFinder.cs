using System.Net;
using System.Net.Sockets;

namespace LosLms.Server;

/// <summary>
/// Finds a free localhost TCP port. Both MySQL and the backend have a preferred default; if the
/// machine already has something on it (another install, a developer's own MySQL), the launcher works
/// around it rather than failing.
/// </summary>
internal static class PortFinder
{
    /// <summary>
    /// Returns <paramref name="preferred"/> if it is free, otherwise the next free port scanning
    /// upward. Bound to 127.0.0.1 only, matching how the services themselves bind.
    /// </summary>
    public static int FindFree(int preferred, int maxAttempts = 200)
    {
        for (var port = preferred; port < preferred + maxAttempts; port++)
        {
            if (IsFree(port))
            {
                return port;
            }
        }

        throw new InvalidOperationException(
            $"No free port found in the range {preferred}-{preferred + maxAttempts}.");
    }

    private static bool IsFree(int port)
    {
        try
        {
            using var listener = new TcpListener(IPAddress.Loopback, port);
            listener.Start();
            listener.Stop();
            return true;
        }
        catch (SocketException)
        {
            return false;
        }
    }
}
