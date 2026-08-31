namespace App_DAL.Entities.Users;

public class User
{
    public Guid Id { get; init; } =  Guid.CreateVersion7();
    public string Name { get; private set; }
    
    public bool IsDeleted { get; private set; } = false;
    
    public DateTime CreatedAt { get; init; }
    public DateTime? UpdatedAt { get; private set; }
    
    public DateTime? DeletedAt { get; private set; }
    
    public User()
    {}
    
    public User(string name)
    {
        Name = name;
        CreatedAt = DateTime.UtcNow;
    }

    public void UpdateUser(string userName)
    {
        Name = userName;
        UpdatedAt = DateTime.UtcNow;
    }
    public void DeleteUser()
    {
        IsDeleted = true;
        DeletedAt = DateTime.UtcNow;
    }
}