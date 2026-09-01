namespace App_DAL.Entities.Authors;

public class Author
{
    public Guid Id { get; init; } =  Guid.CreateVersion7();
    public string Name { get; private set; }
    
    public bool IsDeleted { get; private set; } = false;
    
    public DateTime CreatedAt { get; init; }
    public DateTime? UpdatedAt { get; private set; }
    
    public DateTime? DeletedAt { get; private set; }
    
    public Author()
    {}
    
    public Author(string name)
    {
        Name = name;
        CreatedAt = DateTime.UtcNow;
    }

    public void UpdateAuthor(string authorName)
    {
        Name = authorName;
        UpdatedAt = DateTime.UtcNow;
    }
    public void DeleteAuthor()
    {
        IsDeleted = true;
        DeletedAt = DateTime.UtcNow;
    }
}