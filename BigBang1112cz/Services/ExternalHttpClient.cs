using System.Net;
using System.Net.Sockets;

namespace BigBang1112cz.Services;

/// <summary>
/// HTTP client for requesting user-supplied URLs. Connections are only allowed to public internet addresses,
/// checked on the resolved IP right before connecting, so DNS rebinding and redirects to internal hosts are covered too.
/// </summary>
public static class ExternalHttpClient
{
    public const string Name = "External";

    private static readonly IPNetwork[] BlockedV4 =
    [
        IPNetwork.Parse("0.0.0.0/8"),       // "this" network
        IPNetwork.Parse("10.0.0.0/8"),      // private
        IPNetwork.Parse("100.64.0.0/10"),   // carrier-grade NAT
        IPNetwork.Parse("127.0.0.0/8"),     // loopback
        IPNetwork.Parse("169.254.0.0/16"),  // link-local, cloud metadata endpoints
        IPNetwork.Parse("172.16.0.0/12"),   // private, Docker networks
        IPNetwork.Parse("192.0.0.0/24"),    // IETF protocol assignments
        IPNetwork.Parse("192.0.2.0/24"),    // documentation
        IPNetwork.Parse("192.168.0.0/16"),  // private
        IPNetwork.Parse("198.18.0.0/15"),   // benchmarking
        IPNetwork.Parse("198.51.100.0/24"), // documentation
        IPNetwork.Parse("203.0.113.0/24"),  // documentation
        IPNetwork.Parse("224.0.0.0/4"),     // multicast
        IPNetwork.Parse("240.0.0.0/4"),     // reserved, broadcast
    ];

    private static readonly IPNetwork GlobalUnicastV6 = IPNetwork.Parse("2000::/3");

    private static readonly IPNetwork[] BlockedV6 =
    [
        IPNetwork.Parse("2001::/32"),     // Teredo
        IPNetwork.Parse("2001:db8::/32"), // documentation
        IPNetwork.Parse("2002::/16"),     // 6to4, embeds arbitrary IPv4 addresses
    ];

    public static void AddExternalHttpClient(this IServiceCollection services)
    {
        services.AddHttpClient(Name, client =>
            {
                client.Timeout = TimeSpan.FromSeconds(10);
            })
            .ConfigurePrimaryHttpMessageHandler(() => new SocketsHttpHandler
            {
                // A proxy would make the connect callback see the proxy address instead of the target
                UseProxy = false,
                MaxAutomaticRedirections = 5,
                ConnectTimeout = TimeSpan.FromSeconds(5),
                ConnectCallback = ConnectAsync,
            });
    }

    public static bool IsPublicAddress(IPAddress address)
    {
        if (address.IsIPv4MappedToIPv6)
        {
            address = address.MapToIPv4();
        }

        return address.AddressFamily switch
        {
            AddressFamily.InterNetwork => !BlockedV4.Any(x => x.Contains(address)),
            AddressFamily.InterNetworkV6 => GlobalUnicastV6.Contains(address) && !BlockedV6.Any(x => x.Contains(address)),
            _ => false
        };
    }

    private static async ValueTask<Stream> ConnectAsync(SocketsHttpConnectionContext context, CancellationToken cancellationToken)
    {
        var host = context.DnsEndPoint.Host;

        var addresses = IPAddress.TryParse(host, out var literal)
            ? [literal]
            : await Dns.GetHostAddressesAsync(host, cancellationToken);

        var publicAddresses = addresses.Where(IsPublicAddress).ToArray();

        if (publicAddresses.Length == 0)
        {
            throw new HttpRequestException($"Host {host} does not resolve to a public address.");
        }

        var socket = new Socket(SocketType.Stream, ProtocolType.Tcp) { NoDelay = true };

        try
        {
            await socket.ConnectAsync(publicAddresses, context.DnsEndPoint.Port, cancellationToken);
            return new NetworkStream(socket, ownsSocket: true);
        }
        catch
        {
            socket.Dispose();
            throw;
        }
    }
}
