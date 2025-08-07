namespace EduShield.Api.Tests;

[TestFixture]
public class UnitTest1
{
    [Test]
    public void Test1()
    {
        // Arrange
        var expected = true;

        // Act
        var actual = true;

        // Assert
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void TestWithMoq_ShouldDemonstrateFramework()
    {
        // Arrange
        var mockService = new Mock<ITestService>();
        mockService.Setup(x => x.GetValue()).Returns("Mocked Value");

        // Act
        var result = mockService.Object.GetValue();

        // Assert
        Assert.That(result, Is.EqualTo("Mocked Value"));
        mockService.Verify(x => x.GetValue(), Times.Once);
    }
}

// Example interface for demonstrating Moq
public interface ITestService
{
    string GetValue();
}
