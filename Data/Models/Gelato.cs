using System.ComponentModel.DataAnnotations;
namespace CoffeeBar.Data.Models;

public class Gelato {
    [Key]
    public int Id { get; set; }
public string name? { get; set; }
    public string? MemberObjID { get; set; }
    public Member Member { get; set; }
    public DateTime Created { get; set; }
} 