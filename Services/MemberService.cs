using MongoDB.Driver;
using MongoDB.Bson;
using CoffeeBar.Data.Models;
using CoffeeBar.Data;
using Microsoft.EntityFrameworkCore;
using System.Linq;

namespace CoffeeBar.Services;

public class MemberService {

    public List<Member>? _members { get; set; }
    
    private PayInfoService? payInfoService;
    private IDbContextFactory<CoffeesContext> _dbContextFactory;

    public MemberService(IDbContextFactory<CoffeesContext> dbContext) {
        _dbContextFactory = dbContext;
        payInfoService =  new PayInfoService(dbContext);
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

    public void Pay(Member member, int paidCoffees) {

        PayInfo payInfo = new PayInfo(member);
        var currentPrice = 0.0;
        using(var context = _dbContextFactory.CreateDbContext()) {
            currentPrice = context.CurrentPrice;
        }
        payInfo.Created = DateTime.Now;
        // payInfo.Member = member;
        payInfo.MemberId = member.Id;
        payInfo.Amount = paidCoffees;
        payInfo.CurrentCoffeePrice = paidCoffees * currentPrice;
        if(payInfo == null) {
            throw new Exception("The PayInfo is nothing.");
        }
        
        // payInfoService.Add(payInfo);
        if (member.PayInfos == null)
            member.PayInfos = new List<PayInfo>();
        member.PayInfos.Add(payInfo);

         using(var context = _dbContextFactory.CreateDbContext()) {
            //  context.PayInfos.Add(payInfo);
             context.Members.Update(member);
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
