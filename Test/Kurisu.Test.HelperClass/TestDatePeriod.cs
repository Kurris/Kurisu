using System.Text.Json;
using Kurisu.AspNetCore.CustomClass.Periods;

namespace Kurisu.Test.HelperClass;

[Trait("customClass", "datePeriod")]
public class TestDatePeriod
{
    /// <summary>
    /// 测试构造函数正常赋值。
    /// </summary>
    [Fact]
    public void Constructor_ShouldSetStartAndEnd()
    {
        var start = DateOnly.Parse("2025-06-01");
        var end = DateOnly.Parse("2025-06-10");
        var period = new DatePeriod(start, end);
        Assert.Equal(start, period.Start);
        Assert.Equal(end, period.End);
    }

    /// <summary>
    /// 测试字符串构造函数。
    /// </summary>
    [Fact]
    public void Constructor_ShouldParseStringDates()
    {
        var period = new DatePeriod("2025-06-01", "2025-06-10");
        Assert.Equal(DateOnly.Parse("2025-06-01"), period.Start);
        Assert.Equal(DateOnly.Parse("2025-06-10"), period.End);
    }

    /// <summary>
    /// 测试构造函数抛出异常（开始日期大于结束日期）。
    /// </summary>
    [Fact]
    public void Constructor_ShouldThrow_WhenStartGreaterThanEnd()
    {
        var start = DateOnly.Parse("2025-06-10");
        var end = DateOnly.Parse("2025-06-01");
        Assert.Throws<ArgumentException>(() => new DatePeriod(start, end));
    }

    /// <summary>
    /// 测试IsPresent方法。
    /// </summary>
    [Theory]
    [InlineData("2025-06-01", "2025-06-10", "2025-06-01", true)]
    [InlineData("2025-06-01", "2025-06-10", "2025-06-10", true)]
    [InlineData("2025-06-01", "2025-06-10", "2025-06-05", true)]
    [InlineData("2025-06-01", "2025-06-10", "2025-05-31", false)]
    [InlineData("2025-06-01", "2025-06-10", "2025-06-11", false)]
    public void IsPresent_ShouldReturnExpected(string s, string e, string value, bool expected)
    {
        var period = new DatePeriod(DateOnly.Parse(s), DateOnly.Parse(e));
        var date = DateOnly.Parse(value);
        Assert.Equal(expected, period.IsPresent(date));
    }

    /// <summary>
    /// 测试IsCrossDay属性。
    /// </summary>
    [Theory]
    [InlineData("2025-06-01", "2025-06-10", true)]
    [InlineData("2025-06-01", "2025-06-01", false)]
    public void IsCrossDay_ShouldReturnExpected(string s, string e, bool expected)
    {
        var period = new DatePeriod(DateOnly.Parse(s), DateOnly.Parse(e));
        Assert.Equal(expected, period.IsCrossDay);
    }

    /// <summary>
    /// 测试IsOverlap方法。
    /// </summary>
    [Theory]
    [InlineData("2025-06-01", "2025-06-10", "2025-06-05", "2025-06-15", true)]
    [InlineData("2025-06-01", "2025-06-10", "2025-05-01", "2025-06-01", true)]
    [InlineData("2025-06-01", "2025-06-10", "2025-06-10", "2025-06-20", true)]
    [InlineData("2025-06-01", "2025-06-10", "2025-06-11", "2025-06-20", false)]
    [InlineData("2025-06-01", "2025-06-10", "2025-05-01", "2025-05-31", false)]
    public void IsOverlap_ShouldReturnExpected(string s1, string e1, string s2, string e2, bool expected)
    {
        var period1 = new DatePeriod(DateOnly.Parse(s1), DateOnly.Parse(e1));
        var period2 = new DatePeriod(DateOnly.Parse(s2), DateOnly.Parse(e2));
        Assert.Equal(expected, period1.IsOverlap(period2));
    }

    /// <summary>
    /// 测试极端日期。
    /// </summary>
    [Fact]
    public void ShouldSupport_MinAndMaxDateOnly()
    {
        var period = new DatePeriod(DateOnly.MinValue, DateOnly.MaxValue);
        Assert.True(period.IsPresent(DateOnly.MinValue));
        Assert.True(period.IsPresent(DateOnly.MaxValue));
    }

    /// <summary>
    /// 测试字符串构造函数传入非法日期字符串时抛出异常。
    /// </summary>
    [Theory]
    [InlineData("2025-06-01", "not-a-date")]
    [InlineData("not-a-date", "2025-06-10")]
    [InlineData("not-a-date", "not-a-date")]
    public void Constructor_ShouldThrow_WhenStringIsInvalid(string s, string e)
    {
        Assert.Throws<FormatException>(() => new DatePeriod(s, e));
    }

    /// <summary>
    /// 测试Start==End时IsPresent仅对该点为true。
    /// </summary>
    [Fact]
    public void IsPresent_ShouldBeTrue_OnlyForSinglePoint_WhenStartEqualsEnd()
    {
        var date = DateOnly.Parse("2025-06-01");
        var period = new DatePeriod(date, date);
        Assert.True(period.IsPresent(date));
        Assert.False(period.IsPresent(date.AddDays(-1)));
        Assert.False(period.IsPresent(date.AddDays(1)));
    }

    /// <summary>
    /// 测试IsOverlap边界：两个区间端点重合。
    /// </summary>
    [Fact]
    public void IsOverlap_ShouldBeTrue_WhenEndEqualsOtherStart()
    {
        var period1 = new DatePeriod("2025-06-01", "2025-06-10");
        var period2 = new DatePeriod("2025-06-10", "2025-06-20");
        Assert.True(period1.IsOverlap(period2));
        Assert.True(period2.IsOverlap(period1));
    }

    /// <summary>
    /// 测试默认DateOnly值（MinValue）行为。
    /// </summary>
    [Fact]
    public void IsPresent_ShouldWork_WhenUsingMinValue()
    {
        var period = new DatePeriod(DateOnly.MinValue, DateOnly.MinValue.AddDays(1));
        Assert.True(period.IsPresent(DateOnly.MinValue));
        Assert.True(period.IsPresent(DateOnly.MinValue.AddDays(1)));
        Assert.False(period.IsPresent(DateOnly.MinValue.AddDays(2)));
    }

    /// <summary>
    /// 测试异常消息内容。
    /// </summary>
    [Fact]
    public void Constructor_ShouldThrowWithCorrectMessage_WhenStartGreaterThanEnd()
    {
        var ex = Assert.Throws<ArgumentException>(() => new DatePeriod(DateOnly.Parse("2025-06-10"), DateOnly.Parse("2025-06-01")));
        Assert.Contains("开始日期不能大于结束日期", ex.Message);
    }

    /// <summary>
    /// 测试DatePeriod的JSON序列化和反序列化。
    /// </summary>
    [Fact]
    public void JsonSerializeAndDeserialize_ShouldWork()
    {
        var period = new DatePeriod(DateOnly.Parse("2025-07-01"), DateOnly.Parse("2025-07-10"));
        var json = JsonSerializer.Serialize(period);
        var deserialized = JsonSerializer.Deserialize<DatePeriod>(json);
        Assert.NotNull(deserialized);
        Assert.Equal(period.Start, deserialized.Start);
        Assert.Equal(period.End, deserialized.End);
    }
}