using CoffeeBar.Data.Models;
using CoffeeBar.Data;
using Microsoft.EntityFrameworkCore;
using System.Linq;

namespace CoffeeBar.Services;

public class CoffeeService {
    
    private PayInfoService payInfoService = null;
    private IDbContextFactory<CoffeesContext> _dbContextFactory;

    public CoffeeService (IDbContextFactory<CoffeesContext> dbContext) {
        _dbContextFactory = dbContext;
        payInfoService = new PayInfoService(dbContext);
    }

    public List<Coffee> GetCoffees(Member member) {
        using(var context = _dbContextFactory.CreateDbContext()) {
            return context.Coffees.Where(x => x.Member.Id == member.Id).ToList();
        }
    }
    public List<Coffee> GetCoffeesById(int id) {
        using(var context = _dbContextFactory.CreateDbContext()) {
            return context.Coffees.Where(x => x.Member.Id == id).ToList();
        }
    }

    public int GetCoffeeAmount(Member member) {
        return GetCoffees(member).Count();
    }

    public int GetOpenCoffeeAmount(Member member) {
        return GetCoffees(member).Where(x => x.CreatedDate > payInfoService.GetLastPayDate(member)).Count();
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

    public async void AddCoffee(Member member) {
        var newCoffee = new Coffee();
        newCoffee.CreatedDate = DateTime.Now;
        newCoffee.Member = member;
        newCoffee.MemberObjID = member.ObjID;

        using(var context = _dbContextFactory.CreateDbContext()) {
            //context.Coffees.Add(newCoffee);
            if (member.Coffees == null) { member.Coffees = new List<Coffee>(); }
            member.Coffees.Add(newCoffee);
            context.Coffees.Add(newCoffee);
            context.Update(member);
            await context.SaveChangesAsync();
        }
    }

    public async void RemoveLastCoffee(Member member) {
        using(var context = _dbContextFactory.CreateDbContext()) {
            var lastCoffee = context.Coffees.OrderBy(x => x.CreatedDate).Last();
            
            
            if(lastCoffee != null) {
                Console.WriteLine("### ### DELETET ITEM ### ###");
                Console.WriteLine($"Created: {lastCoffee.CreatedDate}");
                Console.WriteLine($"Created: {lastCoffee.Member.Name}");
                context.Coffees.Remove(lastCoffee);
                await context.SaveChangesAsync();
            } else
                Console.WriteLine("### ### Cannot fnd the last one ### ###");
        }
    }
}