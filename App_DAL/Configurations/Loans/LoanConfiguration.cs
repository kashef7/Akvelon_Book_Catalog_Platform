using App_DAL.Entities.Loans;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace App_DAL.Configurations.Loans;

public class LoanConfiguration: IEntityTypeConfiguration<Loan>
{
    public void Configure(EntityTypeBuilder<Loan> builder)
    {
        builder.HasKey(x => x.Id);
        
        builder.HasOne(x => x.Book)
            .WithMany()
            .HasForeignKey(x => x.BookId)
            .OnDelete(DeleteBehavior.Restrict);
        
        builder.HasOne(x => x.User)
            .WithMany()
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.Restrict);
        
        builder.Property(x => x.LoanedAt).IsRequired();
        builder.Property(x => x.DueAt).IsRequired();
        builder.Property(x => x.BookId).IsRequired();
        builder.Property(x => x.UserId).IsRequired();
        
        builder.HasIndex(l => l.BookId)
            .IsUnique()
            .HasFilter("[ReturnedAt] IS NULL");
        
        builder.HasIndex(x => x.UserId);
        builder.HasIndex(x => x.DueAt);
    }
}