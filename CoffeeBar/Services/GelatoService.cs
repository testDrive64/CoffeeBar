using CoffeeBar.Data.Models;
using CoffeeBar.Data;
using Microsoft.EntityFrameworkCore;
using System.Linq;

namespace CoffeeBar.Services;

public class GelatoService {

    private PayInfoService payInfoService;
    private IDbContextFactory<CoffeesContext> _dbContextFactory;

    public GelatoService (IDbContextFactory<CoffeesContext> dbContext) {
        _dbContextFactory = dbContext;
        payInfoService = new PayInfoService(dbContext);
    }

    public List<Gelato> GetGelatos(Member member) {
        return GetGelatosById(member.Id);
    }
    public List<Gelato> GetGelatosById(int id) {
        using(var context = _dbContextFactory.CreateDbContext()) {
            return context.Gelatos.Where(x => x.MemberId == id).OrderBy(x => x.CreatedDate).ToList();
        }
    }

    public int GetAmount(Member member) {
        using(var context = _dbContextFactory.CreateDbContext()) {
            return context.Gelatos.Count(x => x.MemberId == member.Id);
        }
    }

    // Counts the gelatos of all members since the given date.
    public int GetGelatoAmountSince(DateTime since) {
        using(var context = _dbContextFactory.CreateDbContext()) {
            return context.Gelatos.Count(x => x.CreatedDate >= since);
        }
    }

    public int GetOpenGelatoAmount(Member member) {
        var lastPayDate = payInfoService.GetLastPayDate(member);
        using(var context = _dbContextFactory.CreateDbContext()) {
            return context.Gelatos.Count(x => x.MemberId == member.Id && x.CreatedDate > lastPayDate);
        }
    }

    public Dictionary<DateTime, int> GetDailyGelatoAmount(Member member) {
        Dictionary<DateTime, int> timeAmountDict = new Dictionary<DateTime, int>();
        foreach (Gelato gelato in this.GetGelatos(member)) {
            if (timeAmountDict.ContainsKey(gelato.CreatedDate.Date)) {
                int oldAmount = timeAmountDict[gelato.CreatedDate.Date];
                timeAmountDict[gelato.CreatedDate.Date] = oldAmount + 1;
            } else {
                timeAmountDict.Add(gelato.CreatedDate.Date, 1);
            }
        }
        return timeAmountDict;
    }

    public async Task AddGelato(Member member) {
        var newGelato = new Gelato();
        newGelato.CreatedDate = DateTime.Now;
        newGelato.MemberId = member.Id;
        newGelato.MemberObjID = member.ObjID;

        using(var context = _dbContextFactory.CreateDbContext()) {
            context.Gelatos.Add(newGelato);
            await context.SaveChangesAsync();
        }
    }

    // Removes the member's latest gelato that has not been paid yet.
    public async Task RemoveLastGelato(Member member) {
        var lastPayDate = payInfoService.GetLastPayDate(member);
        using(var context = _dbContextFactory.CreateDbContext()) {
            var lastGelato = context.Gelatos
                                .Where(x => x.MemberId == member.Id && x.CreatedDate > lastPayDate)
                                .OrderBy(x => x.CreatedDate)
                                .LastOrDefault();

            if(lastGelato != null) {
                context.Gelatos.Remove(lastGelato);
                await context.SaveChangesAsync();
            }
        }
    }
}
