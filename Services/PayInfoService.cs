using CoffeeBar.Data.Models;
using CoffeeBar.Data;
using Microsoft.EntityFrameworkCore;
using System.Linq;

namespace CoffeeBar.Services;

public class PayInfoService {

    public List<PayInfo>? _PayInfo{ get; set; }
    private IDbContextFactory<CoffeesContext> _dbContextFactory;

    public PayInfoService (IDbContextFactory<CoffeesContext> dbContext) {
        _dbContextFactory = dbContext;
    }

    public void Add(PayInfo payInfo) {
        using(var context = _dbContextFactory.CreateDbContext()) {
            context.PayInfos.Add(payInfo);
            context.SaveChanges();
        }
    }
    public List<PayInfo> GetPayInfos() {
        using(var context = _dbContextFactory.CreateDbContext()) {
            return context.PayInfos.ToList<PayInfo>();
        }
    }
    public PayInfo GetPayInfo(Member member) {

        using(var context = _dbContextFactory.CreateDbContext()) {
            var membersPayInfo = context.PayInfos.SingleOrDefault(x => x.MemberId == member.Id);
            if( membersPayInfo == null)
                return new PayInfo(member);
                // throw new Exception($"Member does not exist. The Id: {memberId} does not exists.");
            return membersPayInfo;  
        }
    }

    public DateTime GetLastPayDate(Member member) {
        PayInfo payInfo = GetPayInfo(member);
        return payInfo.Created;
    }

}