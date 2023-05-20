using Microsoft.EntityFrameworkCore;
using CoffeeBar.Data.Models;

namespace CoffeeBar.Data;

public class CoffeesContext : DbContext {
    public CoffeesContext(DbContextOptions<CoffeesContext> options) : base(options) {

    }

    public double CurrentPrice = .2;
    public DbSet<Member> Members { get; set; }
    public DbSet<Coffee> Coffees { get; set; }
    public DbSet<PayInfo> PayInfos { get; set; }
}