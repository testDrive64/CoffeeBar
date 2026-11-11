using CoffeeBar.Data.Models;
using CoffeeBar.Data;
using Microsoft.EntityFrameworkCore;
using System.Linq;

namespace CoffeeBar.Services;

public enum DeleteMemberResult {
    Deleted,
    HasOpenCosts,
    NotFound
}

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

    public Member? FindMember(int id) {
        using(var context = _dbContextFactory.CreateDbContext()) {
            return context.Members.SingleOrDefault(x => x.Id == id);
        }
    }

    // Deletes the member with all coffees, gelatos and payments.
    // Nothing is deleted while the member still has open (unpaid) coffees or gelatos.
    public DeleteMemberResult DeleteMember(int id) {
        using(var context = _dbContextFactory.CreateDbContext()) {
            using var transaction = context.Database.BeginTransaction();

            if (!context.Members.Any(x => x.Id == id))
                return DeleteMemberResult.NotFound;

            var lastPayDate = context.PayInfos
                                .Where(x => x.MemberId == id)
                                .OrderByDescending(x => x.Created)
                                .Select(x => x.Created)
                                .FirstOrDefault();
            bool hasOpenCosts = context.Coffees.Any(x => x.MemberId == id && x.CreatedDate > lastPayDate)
                             || context.Gelatos.Any(x => x.MemberId == id && x.CreatedDate > lastPayDate);
            if (hasOpenCosts)
                return DeleteMemberResult.HasOpenCosts;

            context.Coffees.Where(x => x.MemberId == id).ExecuteDelete();
            context.Gelatos.Where(x => x.MemberId == id).ExecuteDelete();
            context.PayInfos.Where(x => x.MemberId == id).ExecuteDelete();
            context.Members.Where(x => x.Id == id).ExecuteDelete();

            transaction.Commit();
            return DeleteMemberResult.Deleted;
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
