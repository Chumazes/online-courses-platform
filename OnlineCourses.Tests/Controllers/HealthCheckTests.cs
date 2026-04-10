using Xunit;

namespace OnlineCourses.Tests.Controllers;

public class HealthCheckTests
{
    [Fact]
    public void Test_AlwaysPasses()
    {
        Assert.True(true);
    }
    
    [Fact]
    public void Test_AdditionWorks()
    {
        Assert.Equal(4, 2 + 2);
    }
}