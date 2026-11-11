using Microsoft.EntityFrameworkCore;
using CoffeeBar.Data.Models;

namespace CoffeeBar.Data;

public class CoffeesContext : DbContext {
    public CoffeesContext(DbContextOptions<CoffeesContext> options) : base(options) {

    }

    public double CurrentCoffeePrice = .2;
    public double CurrentGelatoPrice = .2;
    public double CurrentHighGelatoPrice = .2;
    public DbSet<Member> Members { get; set; }
    public DbSet<Coffee> Coffees { get; set; }
    public DbSet<Gelato> Gelatos { get; set; }
    public DbSet<PayInfo> PayInfos { get; set; }
}