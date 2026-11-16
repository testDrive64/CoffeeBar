using System.Text.Json;
using Bunit;
using CoffeeBar.Data;
using CoffeeBar.Data.Models;
using CoffeeBar.Pages;
using CoffeeBar.Services;
using CoffeeBar.Shared;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace CoffeeBar.Tests;

// Checks what is handed to Chart.js (wwwroot/js/charts.js) without a browser.
public class ChartTests : BunitContext
{
    private readonly TestDbContextFactory _factory = new();
    private readonly MemberService _members;

    public ChartTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddSingleton<IDbContextFactory<CoffeesContext>>(_factory);
        Services.AddTransient<MemberService>();
        Services.AddTransient<CoffeeService>();
        Services.AddTransient<GelatoService>();
        Services.AddTransient<PayInfoService>();
        Services.AddSingleton<DialogService>();
        _members = new MemberService(_factory);
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (disposing)
            _factory.Dispose();
    }

    // Serialized like Blazor's JS interop does it (web defaults, camelCase).
    private static JsonElement ToJson(object config) =>
        JsonSerializer.SerializeToElement(config, new JsonSerializerOptions(JsonSerializerDefaults.Web));

    private IReadOnlyList<JSRuntimeInvocation> Invocations(string identifier) =>
        JSInterop.Invocations.Where(x => x.Identifier == identifier).ToList();

    // ---------- ChartCanvas ----------

    [Fact]
    public void ChartCanvas_RendersOnce_PerConfig()
    {
        var config = new { type = "line" };
        var chart = Render<ChartCanvas>(p => p.Add(x => x.Config, config));

        chart.Render(p => p.Add(x => x.Config, config)); // same object: no redraw
        Assert.Single(Invocations("coffeeCharts.render"));

        chart.Render(p => p.Add(x => x.Config, new { type = "bar" })); // new object: redraw
        Assert.Equal(2, Invocations("coffeeCharts.render").Count);
    }

    [Fact]
    public async Task ChartCanvas_Dispose_DestroysChart()
    {
        Render<ChartCanvas>(p => p.Add(x => x.Config, new { type = "line" }));

        await DisposeComponentsAsync();

        Assert.Single(Invocations("coffeeCharts.destroy"));
    }

    // ---------- Member details chart ----------

    [Fact]
    public void MemberDetail_PassesChartJs4Config()
    {
        _members.AddMember(new Member { Name = "Alex", CreatedDate = DateTime.Now });
        var alex = _members.GetMembers().Single();
        using (var context = _factory.CreateDbContext())
        {
            context.Coffees.AddRange(
                new Coffee { MemberId = alex.Id, CreatedDate = new DateTime(2023, 5, 28, 9, 0, 0) },
                new Coffee { MemberId = alex.Id, CreatedDate = new DateTime(2023, 5, 28, 14, 0, 0) },
                new Coffee { MemberId = alex.Id, CreatedDate = new DateTime(2023, 5, 1, 9, 0, 0) });
            context.PayInfos.Add(new PayInfo { MemberId = alex.Id, Created = new DateTime(2023, 5, 29, 17, 30, 0), Amount = 3 });
            context.SaveChanges();
        }

        Render<MemberDetail>(p => p.Add(x => x.Id, alex.Id));

        var invocation = Assert.Single(Invocations("coffeeCharts.render"));
        var config = ToJson(invocation.Arguments[1]!);

        Assert.Equal("line", config.GetProperty("type").GetString());

        var datasets = config.GetProperty("data").GetProperty("datasets");
        var coffees = datasets[0];
        Assert.Equal("Coffees per day", coffees.GetProperty("label").GetString());
        Assert.Equal("#B06A2C", coffees.GetProperty("borderColor").GetString());
        var points = coffees.GetProperty("data");
        Assert.Equal(2, points.GetArrayLength());
        Assert.Equal("2023-05-01T00:00:00", points[0].GetProperty("x").GetString()); // sorted, no time zone
        Assert.Equal(1, points[0].GetProperty("y").GetDouble());
        Assert.Equal("2023-05-28T00:00:00", points[1].GetProperty("x").GetString());
        Assert.Equal(2, points[1].GetProperty("y").GetDouble());

        var paid = datasets[1];
        Assert.Equal("Paid coffees", paid.GetProperty("label").GetString());
        Assert.False(paid.GetProperty("showLine").GetBoolean());
        Assert.Equal("rectRot", paid.GetProperty("pointStyle").GetString());
        Assert.Equal("2023-05-29T17:30:00", paid.GetProperty("data")[0].GetProperty("x").GetString());

        var x = config.GetProperty("options").GetProperty("scales").GetProperty("x");
        Assert.Equal("time", x.GetProperty("type").GetString());
        // date-fns tokens (moment's DD.MM.YYYY would throw in date-fns)
        Assert.Equal("dd.MM.yyyy", x.GetProperty("time").GetProperty("tooltipFormat").GetString());
        Assert.Equal("dd.MM.yyyy", x.GetProperty("time").GetProperty("displayFormats").GetProperty("day").GetString());

        var y = config.GetProperty("options").GetProperty("scales").GetProperty("y");
        Assert.True(y.GetProperty("beginAtZero").GetBoolean());
        Assert.Equal(0, y.GetProperty("ticks").GetProperty("precision").GetInt32());
    }

    [Fact]
    public void MemberDetail_WithoutData_ShowsNoChart()
    {
        _members.AddMember(new Member { Name = "Alex", CreatedDate = DateTime.Now });
        var alex = _members.GetMembers().Single();

        var page = Render<MemberDetail>(p => p.Add(x => x.Id, alex.Id));

        Assert.Empty(page.FindAll("canvas"));
        Assert.Empty(Invocations("coffeeCharts.render"));
    }

    // ---------- Home trend chart ----------

    [Fact]
    public void Index_PassesTrendConfig_ForLast30Days()
    {
        _members.AddMember(new Member { Name = "Alex", CreatedDate = DateTime.Now });
        _members.AddMember(new Member { Name = "Bo", CreatedDate = DateTime.Now });
        var members = _members.GetMembers();
        using (var context = _factory.CreateDbContext())
        {
            context.Coffees.AddRange(
                new Coffee { MemberId = members[0].Id, CreatedDate = DateTime.Today.AddHours(9) },
                new Coffee { MemberId = members[1].Id, CreatedDate = DateTime.Today.AddHours(10) },
                // Before the period: the first only counts for the average of the first day, the second not at all
                new Coffee { MemberId = members[0].Id, CreatedDate = DateTime.Today.AddDays(-35).AddHours(9) },
                new Coffee { MemberId = members[0].Id, CreatedDate = DateTime.Today.AddDays(-36).AddHours(9) });
            context.SaveChanges();
        }

        var page = Render<CoffeeBar.Pages.Index>();

        Assert.Contains("2 coffees in the last 30 days", page.Find(".cb-trend-summary").TextContent);
        var config = ToJson(Assert.Single(Invocations("coffeeCharts.render")).Arguments[1]!);
        Assert.Equal("bar", config.GetProperty("type").GetString());

        var datasets = config.GetProperty("data").GetProperty("datasets");
        var average = datasets[0];
        Assert.Equal("line", average.GetProperty("type").GetString());
        Assert.Equal("7-day average", average.GetProperty("label").GetString());
        var perDay = datasets[1];
        Assert.Equal("Coffees per day", perDay.GetProperty("label").GetString());
        Assert.Equal("#B06A2C", perDay.GetProperty("backgroundColor").GetString());

        var bars = perDay.GetProperty("data");
        Assert.Equal(30, bars.GetArrayLength()); // one bar per day, also without coffee
        Assert.Equal(DateTime.Today.AddDays(-29).ToString("yyyy-MM-dd'T'00:00:00"), bars[0].GetProperty("x").GetString());
        Assert.Equal(0, bars[0].GetProperty("y").GetDouble());
        Assert.Equal(2, bars[29].GetProperty("y").GetDouble());

        var averages = average.GetProperty("data");
        Assert.Equal(0.1, averages[0].GetProperty("y").GetDouble()); // 1 coffee in the 7 days up to the first day
        Assert.Equal(0.3, averages[29].GetProperty("y").GetDouble()); // 2 / 7

        var x = config.GetProperty("options").GetProperty("scales").GetProperty("x");
        Assert.Equal("day", x.GetProperty("time").GetProperty("unit").GetString());
    }

    [Fact]
    public void Index_SwitchingPeriod_RedrawsChart()
    {
        _members.AddMember(new Member { Name = "Alex", CreatedDate = DateTime.Now });
        using (var context = _factory.CreateDbContext())
        {
            context.Coffees.Add(new Coffee { MemberId = _members.GetMembers().Single().Id, CreatedDate = DateTime.Today.AddDays(-60) });
            context.SaveChanges();
        }

        var page = Render<CoffeeBar.Pages.Index>();
        Assert.Contains("No coffees in the last 30 days", page.Markup);
        Assert.Empty(Invocations("coffeeCharts.render"));

        page.FindAll(".cb-segment").Single(b => b.TextContent == "90 days").Click();

        Assert.Equal("true", page.FindAll(".cb-segment").Single(b => b.TextContent == "90 days").GetAttribute("aria-pressed"));
        var config = ToJson(Assert.Single(Invocations("coffeeCharts.render")).Arguments[1]!);
        var datasets = config.GetProperty("data").GetProperty("datasets");
        Assert.Equal("4-week average", datasets[0].GetProperty("label").GetString());
        Assert.Equal("Coffees per week", datasets[1].GetProperty("label").GetString());

        // One bar per week, starting on Monday, from the week of the first day up to the current week
        var bars = datasets[1].GetProperty("data").EnumerateArray()
            .Select(bar => (Week: DateTime.Parse(bar.GetProperty("x").GetString()!), Coffees: bar.GetProperty("y").GetDouble()))
            .ToList();
        Assert.All(bars, bar => Assert.Equal(DayOfWeek.Monday, bar.Week.DayOfWeek));
        Assert.True(bars[0].Week <= DateTime.Today.AddDays(-89) && bars[0].Week > DateTime.Today.AddDays(-96));
        Assert.True(bars[^1].Week <= DateTime.Today && bars[^1].Week > DateTime.Today.AddDays(-7));
        Assert.Equal(1, bars.Sum(bar => bar.Coffees));
        Assert.Equal("week", config.GetProperty("options").GetProperty("scales").GetProperty("x").GetProperty("time").GetProperty("unit").GetString());
    }
}
