using CoffeeBar.Data.Models;
using CoffeeBar.Data;
using Microsoft.EntityFrameworkCore;
using System.Linq;

namespace CoffeeBar.Services;

public class CoffeeService {

    private PayInfoService payInfoService;
    private IDbContextFactory<CoffeesContext> _dbContextFactory;

    public CoffeeService (IDbContextFactory<CoffeesContext> dbContext) {
        _dbContextFactory = dbContext;
        payInfoService = new PayInfoService(dbContext);
    }

    public List<Coffee> GetCoffees(Member member) {
        return GetCoffeesById(member.Id);
    }
    public List<Coffee> GetCoffeesById(int id) {
        using(var context = _dbContextFactory.CreateDbContext()) {
            return context.Coffees.Where(x => x.MemberId == id).OrderBy(x => x.CreatedDate).ToList();
        }
    }

    public int GetCoffeeAmount(Member member) {
        using(var context = _dbContextFactory.CreateDbContext()) {
            return context.Coffees.Count(x => x.MemberId == member.Id);
        }
    }

    // Counts the coffees of all members since the given date.
    public int GetCoffeeAmountSince(DateTime since) {
        using(var context = _dbContextFactory.CreateDbContext()) {
            return context.Coffees.Count(x => x.CreatedDate >= since);
        }
    }

    public int GetOpenCoffeeAmount(Member member) {
        var lastPayDate = payInfoService.GetLastPayDate(member);
        using(var context = _dbContextFactory.CreateDbContext()) {
            return context.Coffees.Count(x => x.MemberId == member.Id && x.CreatedDate > lastPayDate);
        }
    }

    public Dictionary<DateTime, int> GetDailyCoffeeAmount(Member member) {
        Dictionary<DateTime, int> timeAmountDict = new Dictionary<DateTime, int>();
        foreach (Coffee coffee in this.GetCoffees(member)) {
            if (timeAmountDict.ContainsKey(coffee.CreatedDate.Date)) {
                int oldAmount = timeAmountDict[coffee.CreatedDate.Date];
                timeAmountDict[coffee.CreatedDate.Date] = oldAmount + 1;
            } else {
                timeAmountDict.Add(coffee.CreatedDate.Date, 1);
            }
        }
        return timeAmountDict;
    }

    public async Task AddCoffee(Member member) {
        var newCoffee = new Coffee();
        newCoffee.CreatedDate = DateTime.Now;
        newCoffee.MemberId = member.Id;
        newCoffee.MemberObjID = member.ObjID;

        using(var context = _dbContextFactory.CreateDbContext()) {
            context.Coffees.Add(newCoffee);
            await context.SaveChangesAsync();
        }
    }

    // Removes the member's latest coffee that has not been paid yet.
    public async Task RemoveLastCoffee(Member member) {
        var lastPayDate = payInfoService.GetLastPayDate(member);
        using(var context = _dbContextFactory.CreateDbContext()) {
            var lastCoffee = context.Coffees
                                .Where(x => x.MemberId == member.Id && x.CreatedDate > lastPayDate)
                                .OrderBy(x => x.CreatedDate)
                                .LastOrDefault();

            if(lastCoffee != null) {
                context.Coffees.Remove(lastCoffee);
                await context.SaveChangesAsync();
            }
        }
    }
}
