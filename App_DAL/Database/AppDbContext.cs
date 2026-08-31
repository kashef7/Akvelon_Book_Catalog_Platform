using App_DAL.Entities.Authors;
using App_DAL.Entities.Books;
using App_DAL.Entities.Loans;
using App_DAL.Entities.Users;
using Microsoft.EntityFrameworkCore;

namespace App_DAL.Database;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions options) : base(options) { }
    
    
    public DbSet<Book> Books { get; set; }
    public DbSet<Author> Authors { get; set; }
    public DbSet<User> Users { get; set; }
    public DbSet<Loan> Loans { get; set; }
    
    
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
    }
}