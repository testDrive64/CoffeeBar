namespace CoffeeBar.Data.Models;
public class TodoItem {
    public int Id { get; set; }
    public string? Title { get; set; }
    public bool IsDone { get; set; } = false;
    public DateTime Created { get; set; } 
}