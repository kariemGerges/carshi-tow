using CarshiTow.Application.Abn;

namespace Application.Tests;

public sealed class AbnChecksumTests
{
    [Fact]
    public void TryValidate_accepts_formatted_known_good_abn_and_normalizes()
    {
        Assert.True(AbnChecksum.TryValidate("51 824 753 556", out var normalized));
        Assert.Equal("51824753556", normalized);
    }

    [Theory]
    [InlineData("")]
    [InlineData("5182475355")] // too short
    [InlineData("51824753557")] // wrong checksum
    [InlineData("ABCDEFGHIJK")] // non-digits only -> fail
    public void TryValidate_rejects_bad_input(string bad)
    {
        Assert.False(AbnChecksum.TryValidate(bad, out _));
    }

    [Fact]
    public void CreateRandomChecksumValidAbn_always_passes_TryValidate()
    {
        for (var seed = 0; seed < 40; seed++)
        {
            var abn = AbnChecksum.CreateRandomChecksumValidAbn(new Random(seed));
            Assert.True(AbnChecksum.TryValidate(abn, out var normalized), $"seed={seed} value={abn}");
            Assert.Equal(11, normalized.Length);
        }
    }
}
