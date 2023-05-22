using System.ComponentModel.DataAnnotations;
namespace CoffeeBar.Data.Models;

public class PayInfo {
    [Key]
    public int Id { get; set; }
    public DateTime Created{ get; set; }
    public int MemberId{ get; set; }
    public Member Member{ get; set; }
    public double Amount{ get; set; }
    public double CurrentCoffeePrice{ get; set; }


    public PayInfo() {
        Created = DateTime.MinValue;
    }

    public PayInfo(Member member) {
        Created = DateTime.MinValue;
        MemberId = member.Id;
        Member = member;
        Amount = 0;
    }
}