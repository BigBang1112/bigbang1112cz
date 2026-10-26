using BigBang1112cz.Services;
using Microsoft.Extensions.DependencyInjection;
using System.Net;

namespace BigBang1112cz.Tests;

public class ExternalHttpClientTests
{
    [Test]
    [Arguments("127.0.0.1")]
    [Arguments("10.1.2.3")]
    [Arguments("172.18.0.5")]
    [Arguments("192.168.1.1")]
    [Arguments("169.254.169.254")]
    [Arguments("100.64.0.1")]
    [Arguments("0.0.0.0")]
    [Arguments("255.255.255.255")]
    [Arguments("::1")]
    [Arguments("::")]
    [Arguments("fe80::1")]
    [Arguments("fd00::1")]
    [Arguments("::ffff:127.0.0.1")]
    [Arguments("::ffff:10.0.0.1")]
    [Arguments("2002:7f00:1::1")]
    [Arguments("2001:db8::1")]
    public async Task IsPublicAddress_RejectsNonPublicAddresses(string address)
    {
        await Assert.That(ExternalHttpClient.IsPublicAddress(IPAddress.Parse(address))).IsFalse();
    }

    [Test]
    [Arguments("1.1.1.1")]
    [Arguments("8.8.8.8")]
    [Arguments("::ffff:1.1.1.1")]
    [Arguments("2606:4700:4700::1111")]
    public async Task IsPublicAddress_AcceptsPublicAddresses(string address)
    {
        await Assert.That(ExternalHttpClient.IsPublicAddress(IPAddress.Parse(address))).IsTrue();
    }

    [Test]
    [Arguments("http://127.0.0.1:1/")]
    [Arguments("http://localhost:1/")]
    [Arguments("http://[::1]:1/")]
    public async Task Client_RefusesToConnectToLocalHosts(string url)
    {
        var services = new ServiceCollection();
        services.AddExternalHttpClient();
        using var provider = services.BuildServiceProvider();
        var http = provider.GetRequiredService<IHttpClientFactory>().CreateClient(ExternalHttpClient.Name);

        var exception = await Assert.That(async () => { await http.GetAsync(url); }).Throws<HttpRequestException>();

        // Distinguishes the block from an ordinary "connection refused"
        await Assert.That(exception!.ToString()).Contains("does not resolve to a public address");
    }
}
