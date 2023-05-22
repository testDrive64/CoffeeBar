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

    public List<Coffee> GetCoffeeList(Member member) {
        using(var context = _dbContextFactory.CreateDbContext()) {
            return context.Coffees.Where(x => x.Member.Id == member.Id).ToList();
        }
    }
    public List<Coffee> GetCoffeeListById(int id) {
        using(var context = _dbContextFactory.CreateDbContext()) {
            return context.Coffees.Where(x => x.Member.Id == id).ToList();
        }
    }


    public int GetCoffeeAmount(Member member) {
        return GetCoffeeList(member).Count();
    }

    public int GetOpenCoffeeAmount(Member member) {
        return GetCoffeeList(member).Where(x => x.CreatedDate > payInfoService.GetLastPayDate(member)).Count();
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
}