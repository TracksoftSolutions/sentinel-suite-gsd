using SentinelSuite.Framework.Domain.Shared.Results;
using Xunit;

namespace SentinelSuite.Framework.Domain.Shared.Tests.Results;

public class ResultErrorTests
{
    [Fact]
    public void ResultStatus_WhenEnumeratingNames_DeclaresExactlyTheNineExpectedMembers()
    {
        var expected = new[]
        {
            "Ok",
            "Error",
            "Invalid",
            "NotFound",
            "Conflict",
            "Forbidden",
            "Unauthorized",
            "Unavailable",
            "CriticalError",
        };

        var names = Enum.GetNames<ResultStatus>();

        Assert.Equal(expected, names);
    }

    [Fact]
    public void Constructor_WhenValidCodeAndMessageProvided_StoresBothUnchanged()
    {
        var error = new Error("Validation.Required", "Field is required");

        Assert.Equal("Validation.Required", error.Code);
        Assert.Equal("Field is required", error.Message);
    }

    [Fact]
    public void Constructor_WhenCodeIsNull_ThrowsArgumentNullException()
    {
        string? code = null;

        Assert.Throws<ArgumentNullException>(() => new Error(code!, "message"));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_WhenCodeIsEmptyOrWhitespace_ThrowsArgumentException(string code)
    {
        Assert.Throws<ArgumentException>(() => new Error(code, "message"));
    }

    [Fact]
    public void Constructor_WhenMessageIsNull_ThrowsArgumentNullException()
    {
        string? message = null;

        Assert.Throws<ArgumentNullException>(() => new Error("code", message!));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_WhenMessageIsEmptyOrWhitespace_ThrowsArgumentException(string message)
    {
        Assert.Throws<ArgumentException>(() => new Error("code", message));
    }

    [Fact]
    public void Equality_WhenCodeAndMessageAreIdentical_InstancesAreEqualWithMatchingHashCode()
    {
        var first = new Error("Validation.Required", "Field is required");
        var second = new Error("Validation.Required", "Field is required");

        Assert.Equal(first, second);
        Assert.True(first == second);
        Assert.Equal(first.GetHashCode(), second.GetHashCode());
    }

    [Fact]
    public void Equality_WhenCodeDiffers_InstancesAreNotEqual()
    {
        var first = new Error("Validation.Required", "Field is required");
        var second = new Error("Validation.Other", "Field is required");

        Assert.NotEqual(first, second);
        Assert.False(first == second);
    }

    [Fact]
    public void Equality_WhenMessageDiffers_InstancesAreNotEqual()
    {
        var first = new Error("Validation.Required", "Field is required");
        var second = new Error("Validation.Required", "Different message");

        Assert.NotEqual(first, second);
        Assert.False(first == second);
    }
}
