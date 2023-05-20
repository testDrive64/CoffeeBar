using System.ComponentModel.DataAnnotations;

namespace CoffeeBar.Data.Models;

public class Member {
    [Key, Required]
    public int Id { get; set; }
    public string? ObjID {get; set;}
    public string? Name { get; set; }
    public int AmountCoffee {
        get {
            if (Coffees !=null) return Coffees.Count();
            else return 0;
        }
    }
    public DateTime CreatedDate { get; set; }
    public List<Coffee>? Coffees { get; set; }
} 