using Kurisu.AspNetCore.CustomClass.Periods;

namespace Kurisu.Test.HelperClass;

[Trait("customClass", "datePeriod")]
public class TestDatePeriod
{
    [Theory]
    [InlineData("2025-03-03", "2025-03-10")]
    [InlineData("2025-03-01", "2025-03-04")]
    public void Create_DatePeriod_HasValue_True(string s, string e)
    {
        var start = DateOnly.Parse(s);
        var end = DateOnly.Parse(e);

        var period1 = new DatePeriod(start, end);
        Assert.True(period1.HasValue);

        var period2 = new DatePeriod
        {
            Start = start,
            End = end
        };
        Assert.True(period2.HasValue);
    }

    [Fact]
    public void Create_DatePeriod_HasValue_False()
    {
        var period2 = new DatePeriod();
        Assert.False(period2.HasValue);
    }

    [Theory]
    [InlineData("2025-02-03", "2025-02-10")]
    [InlineData("2025-03-01", "2025-03-03")]
    [InlineData("2025-03-16", "2025-03-16")]
    [InlineData("2025-03-17", "2025-04-16")]
    public void Create_DatePeriod_IsOverlap_False(string s, string e)
    {
        var start = DateOnly.Parse(s);
        var end = DateOnly.Parse(e);

        var period2 = new DatePeriod(DateOnly.Parse("2025-03-04"), DateOnly.Parse("2025-03-15"));
        Assert.False(period2.IsOverlap(new DatePeriod(start, end)));
    }


    [Theory]
    [InlineData("2025-03-04", "2025-03-04")]
    [InlineData("2025-03-04", "2025-03-15")]
    [InlineData("2025-03-04", "2025-03-16")]
    [InlineData("2025-03-05", "2025-03-14")]
    [InlineData("2025-03-15", "2025-04-15")]
    [InlineData("2025-03-15", "2025-04-01")]
    [InlineData("2025-03-14", "2025-03-15")]
    [InlineData("2025-03-13", "2025-03-16")]
    public void Create_DatePeriod_IsOverlap_True(string s, string e)
    {
        var start = DateOnly.Parse(s);
        var end = DateOnly.Parse(e);

        var period2 = new DatePeriod(DateOnly.Parse("2025-03-04"), DateOnly.Parse("2025-03-15"));
        Assert.True(period2.IsOverlap(new DatePeriod(start, end)));
    }

    [Theory]
    [InlineData("2025-03-04", "2025-03-05")]
    [InlineData("2025-04-05", "2025-03-03")]
    public void Create_DatePeriod_IsCrossDay_True(string s, string e)
    {
        var start = DateOnly.Parse(s);
        var end = DateOnly.Parse(e);

        var period2 = new DatePeriod(start, end);
        Assert.True(period2.IsCrossDay);
    }

    [Theory]
    [InlineData("2025-03-04", "2025-03-04")]
    [InlineData("2025-04-05", "2025-04-05")]
    public void Create_DatePeriod_IsCrossDay_False(string s, string e)
    {
        var start = DateOnly.Parse(s);
        var end = DateOnly.Parse(e);

        var period2 = new DatePeriod(start, end);
        Assert.False(period2.IsCrossDay);
    }

    [Theory]
    [InlineData("2025-03-15")]
    [InlineData("2025-03-16")]
    [InlineData("2025-03-17")]
    [InlineData("2025-03-18")]
    [InlineData("2025-03-19")]
    [InlineData("2025-03-20")]
    public void Create_DatePeriod_IsPresent_True(string s)
    {
        var date = DateOnly.Parse(s);
        Assert.True(new DatePeriod("2025-03-15", "2025-03-20").IsPresent(date));
    }
    
    
    [Theory]
    [InlineData("2025-03-14")]
    [InlineData("2025-03-21")]
    public void Create_DatePeriod_IsPresent_False(string s)
    {
        var date = DateOnly.Parse(s);
        Assert.False(new DatePeriod("2025-03-15", "2025-03-20").IsPresent(date));
    }
}