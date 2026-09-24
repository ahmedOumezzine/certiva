using Certiva.Application.Security;
using Xunit;

namespace Certiva.Application.Tests;

public sealed class GuestAttemptTokenServiceTests
{
    [Fact]
    public void GeneratesDistinctUrlSafeTokensAndDeterministicHashes()
    {
        var service = new GuestAttemptTokenService();
        var first = service.GenerateToken();
        var second = service.GenerateToken();

        Assert.False(string.IsNullOrWhiteSpace(first));
        Assert.NotEqual(first, second);
        Assert.DoesNotContain("+", first);
        Assert.DoesNotContain("/", first);
        Assert.Equal(service.HashToken(first), service.HashToken(first));
        Assert.True(service.Matches(first, service.HashToken(first)));
        Assert.False(service.Matches(second, service.HashToken(first)));
        Assert.False(service.Matches(first, "not-a-hex-hash"));
    }
}
