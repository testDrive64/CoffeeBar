using System.ComponentModel.DataAnnotations;
namespace CoffeeBar.Data.Models;

public class Coffee {
    [Key]
    public int Id { get; set;}
    public string? ObjID { get; set; }
    public string? MemberObjID { get; set; }
    public Member Member { get; set; }
    public DateTime CreatedDate { get; set; }
} 