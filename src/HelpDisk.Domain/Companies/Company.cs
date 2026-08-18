using HelpDisk.Domain.Primitives;

namespace HelpDisk.Domain.Companies;

public sealed class Company : Entity<Guid>
{
    public string Name { get; private set; }

    public Company(string name)
    {
        Id = Guid.NewGuid();
        Name = name;
    }

    private Company()
    {
        Name = string.Empty;
    }
}