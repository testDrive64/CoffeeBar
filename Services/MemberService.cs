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

    public double CoffeePrice {
        get {
            using(var context = _dbContextFactory.CreateDbContext()) {
                return context.CurrentCoffeePrice;
            }
        }
    }

    public double GelatoPrice {
        get {
            using(var context = _dbContextFactory.CreateDbContext()) {
                return context.CurrentGelatoPrice;
            }
        }
    }

    // Pays all open coffees and gelatos of the member with one PayInfo.
    public void Pay(Member member, int paidCoffees, int paidGelatos) {
        if (paidCoffees <= 0 && paidGelatos <= 0)
            return;

        using(var context = _dbContextFactory.CreateDbContext()) {
            PayInfo payInfo = new PayInfo();
            payInfo.Created = DateTime.Now;
            payInfo.MemberId = member.Id;
            payInfo.Amount = paidCoffees;
            // Hold the total prices of the paid coffees and gelatos.
            payInfo.CurrentCoffeePrice = paidCoffees * context.CurrentCoffeePrice;
            payInfo.CurrentGelatoPrice = paidGelatos * context.CurrentGelatoPrice;

            context.PayInfos.Add(payInfo);
            context.SaveChanges();
        }
    }
}
