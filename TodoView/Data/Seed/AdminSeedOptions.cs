namespace TodoView.Data.Seed;

public class AdminSeedOptions
{
    public const string SectionName = "SeedAdmin";

    public string Email { get; set; } = "admin@todoview.local";
    public string Password { get; set; } = "Admin123!";
    public string FirstName { get; set; } = "System";
    public string LastName { get; set; } = "Admin";
    public string Address { get; set; } = "Admin Office";
}
