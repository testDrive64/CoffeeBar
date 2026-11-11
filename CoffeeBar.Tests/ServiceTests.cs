using CoffeeBar.Data.Models;
using CoffeeBar.Services;

namespace CoffeeBar.Tests;

public class ServiceTests : IDisposable
{
    private readonly TestDbContextFactory _factory = new();
    private readonly MemberService _members;
    private readonly CoffeeService _coffees;
    private readonly GelatoService _gelatos;
    private readonly PayInfoService _payInfos;

    public ServiceTests()
    {
        _members = new MemberService(_factory);
        _coffees = new CoffeeService(_factory);
        _gelatos = new GelatoService(_factory);
        _payInfos = new PayInfoService(_factory);
    }

    public void Dispose() => _factory.Dispose();

    private Member NewMember(string name)
    {
        _members.AddMember(new Member { Name = name, CreatedDate = DateTime.Now });
        return _members.GetMembers().Single(m => m.Name == name);
    }

    // ---------- Coffee ----------

    [Fact]
    public async Task AddCoffee_IsPersistedImmediately()
    {
        var a = NewMember("A");
        await _coffees.AddCoffee(a);
        await _coffees.AddCoffee(a);

        Assert.Equal(2, _coffees.GetCoffeeAmount(a));
        Assert.Equal(2, _coffees.GetOpenCoffeeAmount(a));
        Assert.Single(_members.GetMembers()); // must not duplicate the member
    }

    [Fact]
    public async Task RemoveLastCoffee_OnlyAffectsThatMember()
    {
        var a = NewMember("A");
        var b = NewMember("B");
        await _coffees.AddCoffee(a);
        await _coffees.AddCoffee(a);
        await _coffees.AddCoffee(b); // the newest coffee overall belongs to B

        await _coffees.RemoveLastCoffee(a);

        Assert.Equal(1, _coffees.GetCoffeeAmount(a));
        Assert.Equal(1, _coffees.GetCoffeeAmount(b));
    }

    [Fact]
    public async Task RemoveLastCoffee_WithoutCoffees_DoesNotThrow()
    {
        var a = NewMember("A");

        await _coffees.RemoveLastCoffee(a);

        Assert.Equal(0, _coffees.GetCoffeeAmount(a));
    }

    [Fact]
    public async Task RemoveLastCoffee_DoesNotRemovePaidCoffees()
    {
        var a = NewMember("A");
        await _coffees.AddCoffee(a);
        _members.Pay(a, 1, 0);

        await _coffees.RemoveLastCoffee(a);

        Assert.Equal(1, _coffees.GetCoffeeAmount(a));
        Assert.Single(_payInfos.GetPayInfos(a));
    }

    [Fact]
    public void DailyAmount_GroupsByDay_Sorted()
    {
        var a = NewMember("A");
        using (var context = _factory.CreateDbContext())
        {
            context.Coffees.AddRange(
                new Coffee { MemberId = a.Id, CreatedDate = new DateTime(2023, 5, 3, 9, 0, 0) },
                new Coffee { MemberId = a.Id, CreatedDate = new DateTime(2023, 5, 1, 9, 0, 0) },
                new Coffee { MemberId = a.Id, CreatedDate = new DateTime(2023, 5, 3, 14, 0, 0) });
            context.SaveChanges();
        }

        var daily = _coffees.GetDailyCoffeeAmount(a);

        Assert.Equal(new[] { new DateTime(2023, 5, 1), new DateTime(2023, 5, 3) }, daily.Keys.ToArray());
        Assert.Equal(new[] { 1, 2 }, daily.Values.ToArray());
    }

    [Fact]
    public async Task AmountSince_CountsAllMembersFromDate()
    {
        var a = NewMember("A");
        var b = NewMember("B");
        using (var context = _factory.CreateDbContext())
        {
            context.Coffees.Add(new Coffee { MemberId = a.Id, CreatedDate = DateTime.Today.AddDays(-1) });
            context.SaveChanges();
        }
        await _coffees.AddCoffee(a);
        await _coffees.AddCoffee(b);
        await _gelatos.AddGelato(b);

        Assert.Equal(2, _coffees.GetCoffeeAmountSince(DateTime.Today));
        Assert.Equal(1, _gelatos.GetGelatoAmountSince(DateTime.Today));
    }

    // ---------- Gelato ----------

    [Fact]
    public async Task Gelato_AddRemove_OnlyAffectsThatMember()
    {
        var a = NewMember("A");
        var b = NewMember("B");
        await _gelatos.AddGelato(a);
        await _gelatos.AddGelato(a);
        await _gelatos.AddGelato(b);

        await _gelatos.RemoveLastGelato(a);
        await _gelatos.RemoveLastGelato(NewMember("C")); // nothing to remove, must not throw

        Assert.Equal(1, _gelatos.GetAmount(a));
        Assert.Equal(1, _gelatos.GetAmount(b));
    }

    // ---------- Pay ----------

    [Fact]
    public async Task Pay_ResetsOpenCounter_KeepsTotal_StoresPrice()
    {
        var a = NewMember("A");
        for (int i = 0; i < 4; i++)
            await _coffees.AddCoffee(a);

        _members.Pay(a, _coffees.GetOpenCoffeeAmount(a), 0);

        Assert.Equal(0, _coffees.GetOpenCoffeeAmount(a));
        Assert.Equal(4, _coffees.GetCoffeeAmount(a));
        var payInfo = Assert.Single(_payInfos.GetPayInfos(a));
        Assert.Equal(4, payInfo.Amount);
        Assert.Equal(4 * _members.CoffeePrice, payInfo.CurrentCoffeePrice, 6);

        await Task.Delay(5);
        await _coffees.AddCoffee(a);
        Assert.Equal(1, _coffees.GetOpenCoffeeAmount(a));
    }

    [Fact]
    public void Pay_WithNothingOpen_CreatesNoPayInfo()
    {
        var a = NewMember("A");

        _members.Pay(a, 0, 0);

        Assert.Empty(_payInfos.GetPayInfos(a));
        Assert.Equal(DateTime.MinValue, _payInfos.GetLastPayDate(a));
    }

    [Fact]
    public async Task PayTwice_WithSameMemberInstance_Works()
    {
        var a = NewMember("A");
        await _coffees.AddCoffee(a);
        _members.Pay(a, 1, 0);
        await Task.Delay(5);
        await _coffees.AddCoffee(a);
        _members.Pay(a, 1, 0);

        Assert.Equal(2, _payInfos.GetPayInfos(a).Count);
        Assert.Equal(0, _coffees.GetOpenCoffeeAmount(a));
    }

    [Fact]
    public async Task Pay_PaysCoffeesAndGelatosTogether()
    {
        var a = NewMember("A");
        await _coffees.AddCoffee(a);
        await _coffees.AddCoffee(a);
        await _gelatos.AddGelato(a);
        await _gelatos.AddGelato(a);
        await _gelatos.AddGelato(a);

        _members.Pay(a, _coffees.GetOpenCoffeeAmount(a), _gelatos.GetOpenGelatoAmount(a));

        Assert.Equal(0, _coffees.GetOpenCoffeeAmount(a));
        Assert.Equal(0, _gelatos.GetOpenGelatoAmount(a));
        var payInfo = Assert.Single(_payInfos.GetPayInfos(a));
        Assert.Equal(2, payInfo.Amount);
        Assert.Equal(2 * _members.CoffeePrice, payInfo.CurrentCoffeePrice, 6);
        Assert.Equal(3 * _members.GelatoPrice, payInfo.CurrentGelatoPrice, 6);
    }

    [Fact]
    public async Task Pay_OnlyGelatos_CreatesPayInfo()
    {
        var a = NewMember("A");
        await _gelatos.AddGelato(a);

        _members.Pay(a, 0, _gelatos.GetOpenGelatoAmount(a));

        Assert.Single(_payInfos.GetPayInfos(a));
        Assert.Equal(0, _gelatos.GetOpenGelatoAmount(a));
    }
}
