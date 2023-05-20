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
            if(member == null)
                throw new Exception($"Member does not exist. The Id: {id} does not exists.");
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

    public void Pay(int id, int paidCoffees) {
        PayInfo payInfo = new PayInfo();
        var currentPrice = 0.0;
        using(var context = _dbContextFactory.CreateDbContext()) {
            currentPrice = context.CurrentPrice;
        }
        payInfo.Created = DateTime.Now;
        payInfo.Member = GetMember(id);
        payInfo.MemberId = id;
        payInfo.Amount = paidCoffees;
        payInfo.CurrentCoffeePrice = paidCoffees * currentPrice;
        using(var context = _dbContextFactory.CreateDbContext()) {
            context.PayInfos.Add(payInfo);
            context.SaveChanges();
        }
    }

    public DateTime GetLastPayDate(int memberId) {
        using(var context = _dbContextFactory.CreateDbContext()) {
            context.PayInfos.Where(x => x.MemberId == memberId);
            context.SaveChanges();
        }
        return DateTime.Now;
    }
}
