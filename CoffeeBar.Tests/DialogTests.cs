using Bunit;
using CoffeeBar.Data;
using CoffeeBar.Data.Models;
using CoffeeBar.Pages;
using CoffeeBar.Services;
using CoffeeBar.Shared;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace CoffeeBar.Tests;

// Renders the in-app dialogs with bUnit (no browser) and clicks through them.
public class DialogTests : BunitContext
{
    private readonly TestDbContextFactory _factory = new();
    private readonly MemberService _members;
    private readonly CoffeeService _coffees;
    private readonly PayInfoService _payInfos;
    private readonly DialogService _dialogs = new();

    public DialogTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose; // focus and chart interop
        Services.AddSingleton<IDbContextFactory<CoffeesContext>>(_factory);
        Services.AddTransient<MemberService>();
        Services.AddTransient<CoffeeService>();
        Services.AddTransient<GelatoService>();
        Services.AddTransient<PayInfoService>();
        Services.AddSingleton(_dialogs);

        _members = new MemberService(_factory);
        _coffees = new CoffeeService(_factory);
        _payInfos = new PayInfoService(_factory);
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (disposing)
            _factory.Dispose();
    }

    private Member NewMember(string name)
    {
        _members.AddMember(new Member { Name = name, CreatedDate = DateTime.Now });
        return _members.GetMembers().Single(m => m.Name == name);
    }

    private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(5);

    // Opens a dialog on the renderer thread, like a component event handler does.
    private async Task<Task<bool>> OpenDialog(IRenderedComponent<DialogHost> host, DialogOptions options)
    {
        Task<bool>? result = null;
        await host.InvokeAsync(() => { result = _dialogs.ConfirmAsync(options); });
        return result!;
    }

    // ---------- DialogService ----------

    [Fact]
    public async Task Service_SecondDialog_CancelsTheFirst()
    {
        var first = _dialogs.ConfirmAsync(new DialogOptions { Title = "First" });
        var second = _dialogs.ConfirmAsync(new DialogOptions { Title = "Second" });

        Assert.False(await first.WaitAsync(Timeout));
        Assert.Equal("Second", _dialogs.Current?.Title);
        _dialogs.Close(true);
        Assert.True(await second.WaitAsync(Timeout));
        Assert.Null(_dialogs.Current);
    }

    // ---------- DialogHost ----------

    [Fact]
    public void Host_RendersNothing_WithoutDialog()
    {
        var host = Render<DialogHost>();

        Assert.Empty(host.FindAll(".cb-dialog"));
    }

    [Fact]
    public async Task Host_ShowsOptions_AndConfirmReturnsTrue()
    {
        var host = Render<DialogHost>();
        var result = await OpenDialog(host, new DialogOptions {
            Title = "Pay for Alex?",
            Details = new() { new DialogDetail("2 × Coffee", "0.40 €") },
            Summary = new DialogDetail("Total", "0.40 €"),
            ConfirmText = "Pay now"
        });

        Assert.Equal("Pay for Alex?", host.Find("#cb-dialog-title").TextContent);
        Assert.Contains("2 × Coffee", host.Find(".cb-dialog-details").TextContent);
        Assert.Contains("Total", host.Find(".cb-dialog-summary").TextContent);
        Assert.Equal("dialog", host.Find(".cb-dialog").GetAttribute("role"));

        await host.Find(".cb-dialog-confirm").ClickAsync(new MouseEventArgs());

        Assert.True(await result.WaitAsync(Timeout));
        Assert.Empty(host.FindAll(".cb-dialog"));
    }

    [Fact]
    public async Task Host_CancelButton_ReturnsFalse()
    {
        var host = Render<DialogHost>();
        var result = await OpenDialog(host, new DialogOptions { Title = "Question" });

        await host.Find(".cb-dialog-cancel").ClickAsync(new MouseEventArgs());

        Assert.False(await result.WaitAsync(Timeout));
        Assert.Empty(host.FindAll(".cb-dialog"));
    }

    [Fact]
    public async Task Host_TapOnBackdrop_Cancels()
    {
        var host = Render<DialogHost>();
        var result = await OpenDialog(host, new DialogOptions { Title = "Question" });

        await host.Find(".cb-dialog-backdrop").ClickAsync(new MouseEventArgs());

        Assert.False(await result.WaitAsync(Timeout));
    }

    [Fact]
    public async Task Host_TapInsideDialog_DoesNotReachTheBackdrop()
    {
        var host = Render<DialogHost>();
        var result = await OpenDialog(host, new DialogOptions { Title = "Question", Message = "Text" });

        // bUnit does not bubble clicks like a browser. Blazor stops the click inside the dialog
        // with this marker, so a tap into the dialog never reaches the backdrop (which cancels).
        Assert.True(host.Find(".cb-dialog").HasAttribute("blazor:onclick:stopPropagation"));
        Assert.False(host.Find(".cb-dialog-backdrop").HasAttribute("blazor:onclick:stopPropagation"));
        Assert.False(result.IsCompleted);
    }

    [Fact]
    public async Task Host_Escape_Cancels()
    {
        var host = Render<DialogHost>();
        var result = await OpenDialog(host, new DialogOptions { Title = "Question" });

        await host.Find(".cb-dialog").KeyDownAsync(new KeyboardEventArgs { Key = "Escape" });

        Assert.False(await result.WaitAsync(Timeout));
    }

    [Fact]
    public async Task Host_Alert_HasOnlyOkButton()
    {
        var host = Render<DialogHost>();
        await host.InvokeAsync(() => { _ = _dialogs.AlertAsync("Nothing to pay", "Nothing open."); });

        Assert.Empty(host.FindAll(".cb-dialog-cancel"));
        Assert.Equal("OK", host.Find(".cb-dialog-confirm").TextContent);
    }

    [Fact]
    public async Task Host_Danger_UsesDangerButton()
    {
        var host = Render<DialogHost>();
        await OpenDialog(host, new DialogOptions { Title = "Delete?", Kind = DialogKind.Danger });

        Assert.Contains("is-danger", host.Find(".cb-dialog").ClassList);
        Assert.Contains("cb-btn-danger", host.Find(".cb-dialog-confirm").ClassList);
    }

    // ---------- Pay ----------

    [Fact]
    public async Task PayButton_Confirm_PaysAllOpen()
    {
        var alex = NewMember("Alex");
        await _coffees.AddCoffee(alex);
        await _coffees.AddCoffee(alex);
        var host = Render<DialogHost>();
        var button = Render<PayButton>(p => p.Add(x => x.member, alex));

        var pay = button.Find("button").ClickAsync(new MouseEventArgs());

        Assert.Equal("Pay for Alex?", host.Find("#cb-dialog-title").TextContent);
        Assert.Contains("2 × Coffee", host.Find(".cb-dialog-details").TextContent);
        Assert.Empty(_payInfos.GetPayInfos(alex)); // nothing paid before confirming

        await host.Find(".cb-dialog-confirm").ClickAsync(new MouseEventArgs());

        await pay.WaitAsync(Timeout);
        Assert.Single(_payInfos.GetPayInfos(alex));
        Assert.Equal(0, _coffees.GetOpenCoffeeAmount(alex));
    }

    [Fact]
    public async Task PayButton_Cancel_PaysNothing()
    {
        var alex = NewMember("Alex");
        await _coffees.AddCoffee(alex);
        var host = Render<DialogHost>();
        var button = Render<PayButton>(p => p.Add(x => x.member, alex));

        var pay = button.Find("button").ClickAsync(new MouseEventArgs());
        await host.Find(".cb-dialog-cancel").ClickAsync(new MouseEventArgs());
        await pay.WaitAsync(Timeout);

        Assert.Empty(_payInfos.GetPayInfos(alex));
        Assert.Equal(1, _coffees.GetOpenCoffeeAmount(alex));
    }

    [Fact]
    public void PayButton_NothingOpen_ShowsMessage()
    {
        var alex = NewMember("Alex");
        var host = Render<DialogHost>();
        var button = Render<PayButton>(p => p.Add(x => x.member, alex));

        _ = button.Find("button").ClickAsync(new MouseEventArgs()); // waits for OK, not clicked here

        Assert.Equal("Nothing to pay", host.Find("#cb-dialog-title").TextContent);
        Assert.Empty(host.FindAll(".cb-dialog-cancel"));
    }

    // ---------- Delete member ----------

    [Fact]
    public async Task MemberDetail_DeleteConfirmed_DeletesAndNavigates()
    {
        var alex = NewMember("Alex");
        var host = Render<DialogHost>();
        var page = Render<MemberDetail>(p => p.Add(x => x.Id, alex.Id));

        var delete = page.Find(".cb-btn-danger").ClickAsync(new MouseEventArgs());

        Assert.Equal("Delete Alex?", host.Find("#cb-dialog-title").TextContent);
        Assert.NotNull(_members.FindMember(alex.Id)); // nothing deleted before confirming

        await host.Find(".cb-dialog-confirm").ClickAsync(new MouseEventArgs());

        await delete.WaitAsync(Timeout);
        Assert.Null(_members.FindMember(alex.Id));
        Assert.EndsWith("/coffeelist", Services.GetRequiredService<NavigationManager>().Uri);
    }

    [Fact]
    public async Task MemberDetail_DeleteCancelled_KeepsMember()
    {
        var alex = NewMember("Alex");
        var host = Render<DialogHost>();
        var page = Render<MemberDetail>(p => p.Add(x => x.Id, alex.Id));

        var delete = page.Find(".cb-btn-danger").ClickAsync(new MouseEventArgs());
        await host.Find(".cb-dialog-cancel").ClickAsync(new MouseEventArgs());
        await delete.WaitAsync(Timeout);

        Assert.NotNull(_members.FindMember(alex.Id));
    }

    [Fact]
    public async Task MemberDetail_OpenCosts_DeleteButtonDisabled()
    {
        var alex = NewMember("Alex");
        await _coffees.AddCoffee(alex);

        var page = Render<MemberDetail>(p => p.Add(x => x.Id, alex.Id));

        Assert.True(page.Find(".cb-btn-danger").HasAttribute("disabled"));
    }
}
