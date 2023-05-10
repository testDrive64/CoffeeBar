using MongoDB.Driver;
using MongoDB.Bson;
using CoffeeBar.Data.Models;
using CoffeeBar.Data;
using Microsoft.EntityFrameworkCore;
using System.Linq;

namespace CoffeeBar.Services;

public class MemberService {

    public List<Member>? _members { get; set; }
    private IDbContextFactory<CoffeesContext> _dbContextFactory;

    public MemberService(IDbContextFactory<CoffeesContext> dbContext) {
        _dbContextFactory = dbContext;
    }

    public void AddMember(Member member) {
        using(var context = _dbContextFactory.CreateDbContext()) {
            context.Members.Add(member);
            context.SaveChanges();
        }
    }
    public List<Member> GetMembers() {
        using(var context = _dbContextFactory.CreateDbContext()) {
            return context.Members.ToList<Member>();
        }
    }
    public Member GetMember(int id) {

        using(var context = _dbContextFactory.CreateDbContext()) {
            var member = context.Members.SingleOrDefault(x => x.Id == id);
            return member;
        }
    }

    public void UpdateMemberByName(int id, string name, int amountCoffee) {
        var customer = GetMember(id);
        if(customer == null) {
            throw new Exception("Member does not exist. Cannot update this member");
        }

        //customer.AmountCoffee = amountCoffee;
        using(var context = _dbContextFactory.CreateDbContext()) {
            context.Update(customer);
            context.SaveChanges();
        }
    }

}
