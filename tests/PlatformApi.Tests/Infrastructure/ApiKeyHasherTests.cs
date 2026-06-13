using AgentPort.PlatformApi.Infrastructure;
using FluentAssertions;
using Xunit;

namespace AgentPort.PlatformApi.Tests.Infrastructure;

// Security-critical: ApiKeyHasher is the only thing standing between a raw API key
// presented at the boundary and the stored credential it is matched against.
// These tests pin the contract that the lookup path in ValidateRequiredApiKeyAsync
// depends on (Hash(rawKey) == stored KeyHash).
public sealed class ApiKeyHasherTests
{
    [Fact]
    public void Hash_IsDeterministic_ForSameInput()
    {
        const string rawKey = "ap_local_0123456789abcdef0123456789abcdef0123456789abcdef0123456789ab";

        var first = ApiKeyHasher.Hash(rawKey);
        var second = ApiKeyHasher.Hash(rawKey);

        first.Should().Be(second);
    }

    [Fact]
    public void Hash_ProducesDistinctValues_ForDifferentInputs()
    {
        var hashA = ApiKeyHasher.Hash("ap_local_aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa");
        var hashB = ApiKeyHasher.Hash("ap_local_bbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbb");

        hashA.Should().NotBe(hashB);
    }

    [Fact]
    public void Hash_IsCaseSensitive_OnInput()
    {
        // The hash must not collapse case; "Key" and "key" are different secrets.
        ApiKeyHasher.Hash("Secret-Key").Should().NotBe(ApiKeyHasher.Hash("secret-key"));
    }

    [Fact]
    public void Hash_ReturnsLowercaseSha256_AsSixtyFourHexChars()
    {
        var hash = ApiKeyHasher.Hash("ap_local_sample");

        hash.Should().HaveLength(64);
        hash.Should().MatchRegex("^[0-9a-f]{64}$");
    }

    [Fact]
    public void Hash_MatchesKnownSha256Vector()
    {
        // SHA-256 of the UTF-8 bytes of "agentport", lowercase hex.
        // Pins the exact algorithm so a refactor cannot silently change it
        // and invalidate every stored key hash.
        var hash = ApiKeyHasher.Hash("agentport");

        hash.Should().Be("3c3d8829cc7943da1bad27b090a0b6601617d1e68db78b4ce95573936240aeaf");
    }

    [Fact]
    public void GetPrefix_ReturnsFirst24Characters_ForLongKey()
    {
        const string rawKey = "ap_local_0123456789abcdef0123456789abcdef";

        var prefix = ApiKeyHasher.GetPrefix(rawKey);

        prefix.Should().HaveLength(24);
        prefix.Should().Be(rawKey[..24]);
        rawKey.Should().StartWith(prefix);
    }

    [Fact]
    public void GetPrefix_ReturnsWholeKey_WhenShorterThan24Characters()
    {
        const string shortKey = "ap_local_short";

        var prefix = ApiKeyHasher.GetPrefix(shortKey);

        prefix.Should().Be(shortKey);
    }

    [Fact]
    public void GenerateRawKey_HasExpectedFormatAndPrefix()
    {
        var rawKey = ApiKeyHasher.GenerateRawKey();

        rawKey.Should().StartWith("ap_local_");
        // 9-char prefix + 64 hex chars (32 random bytes).
        rawKey.Should().HaveLength("ap_local_".Length + 64);
        rawKey["ap_local_".Length..].Should().MatchRegex("^[0-9a-f]{64}$");
    }

    [Fact]
    public void GenerateRawKey_ProducesUniqueValues()
    {
        var keys = Enumerable.Range(0, 50).Select(_ => ApiKeyHasher.GenerateRawKey()).ToList();

        keys.Distinct().Should().HaveCount(keys.Count);
    }

    [Fact]
    public void GenerateRawKey_ProducesKeyWhosePrefixIsItsFirst24Characters()
    {
        var rawKey = ApiKeyHasher.GenerateRawKey();

        ApiKeyHasher.GetPrefix(rawKey).Should().Be(rawKey[..24]);
    }
}
