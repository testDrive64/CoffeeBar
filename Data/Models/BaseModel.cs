using System.ComponentModel.DataAnnotations;
namespace CoffeeBar.Data.Models;

public class BaseModel {
    [Key]
    public int Id { get; set;}
    public string? ObjID { get; set; }
    public string? MemberObjID { get; set; }
    public Member Member { get; set; }
    public DateTime CreatedDate { get; set; }
} 