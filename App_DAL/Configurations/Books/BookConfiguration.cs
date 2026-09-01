using App_DAL.Entities.Books;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace App_DAL.Configurations.Books;

public class BookConfiguration : IEntityTypeConfiguration<Book>
{
    public void Configure(EntityTypeBuilder<Book> builder)
    {
        builder.HasKey(x => x.Id);
        builder.HasOne(x => x.Author)
            .WithMany()
            .HasForeignKey(b => b.AuthorId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.Property(x => x.Isbn).IsRequired().HasMaxLength(13);
        builder.Property(x => x.Title).IsRequired().HasMaxLength(100);
        builder.Property(x => x.Description).IsRequired().HasMaxLength(1000);
        builder.Property(x=> x.DatePublished).IsRequired();
        builder.Property(x => x.Rating).IsRequired().HasPrecision(3, 2);
        builder.Property(x=> x.AuthorId).IsRequired();
        builder.Property(x=> x.IsDeleted).IsRequired().HasDefaultValue(false);
        builder.Property(x=> x.CreatedAt).IsRequired();
        
        builder.HasIndex(x => x.AuthorId);
        builder.HasIndex(x => x.DatePublished);
        builder.HasIndex(x => x.Rating);
        builder.HasIndex(x => x.Isbn)
            .IsUnique();
    }
}