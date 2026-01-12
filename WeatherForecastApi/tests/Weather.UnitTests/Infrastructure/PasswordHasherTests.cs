using FluentAssertions;
using Weather.Infrastructure.Services;

namespace Weather.UnitTests.Infrastructure;

public class PasswordHasherTests
{
    private readonly BcryptPasswordHasher _passwordHasher;

    public PasswordHasherTests()
    {
        _passwordHasher = new BcryptPasswordHasher();
    }

    [Fact]
    public void Hash_ReturnsNonEmptyString()
    {
        // Arrange
        var password = "testPassword123";

        // Act
        var hash = _passwordHasher.Hash(password);

        // Assert
        hash.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public void Hash_ReturnsDifferentHashForSamePassword()
    {
        // Arrange
        var password = "testPassword123";

        // Act
        var hash1 = _passwordHasher.Hash(password);
        var hash2 = _passwordHasher.Hash(password);

        // Assert (BCrypt generates different hashes due to salt)
        hash1.Should().NotBe(hash2);
    }

    [Fact]
    public void Verify_WithCorrectPassword_ReturnsTrue()
    {
        // Arrange
        var password = "testPassword123";
        var hash = _passwordHasher.Hash(password);

        // Act
        var result = _passwordHasher.Verify(password, hash);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void Verify_WithIncorrectPassword_ReturnsFalse()
    {
        // Arrange
        var password = "testPassword123";
        var wrongPassword = "wrongPassword456";
        var hash = _passwordHasher.Hash(password);

        // Act
        var result = _passwordHasher.Verify(wrongPassword, hash);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void Verify_WithDifferentCasePassword_ReturnsFalse()
    {
        // Arrange
        var password = "TestPassword123";
        var differentCasePassword = "testpassword123";
        var hash = _passwordHasher.Hash(password);

        // Act
        var result = _passwordHasher.Verify(differentCasePassword, hash);

        // Assert
        result.Should().BeFalse();
    }

    [Theory]
    [InlineData("short")]
    [InlineData("VeryLongPasswordThatShouldStillWorkCorrectlyWithBCryptHashing123!@#")]
    [InlineData("Password with spaces")]
    [InlineData("P@$$w0rd!#$%")]
    public void Hash_AndVerify_WorksWithVariousPasswordFormats(string password)
    {
        // Arrange & Act
        var hash = _passwordHasher.Hash(password);
        var verifyResult = _passwordHasher.Verify(password, hash);

        // Assert
        hash.Should().NotBeNullOrEmpty();
        verifyResult.Should().BeTrue();
    }
}
