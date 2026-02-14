using Forssa.Domain.Entities;

namespace Forssa.Domain.Entities;

// Example entity that inherits from BaseEntity
public class Product : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public int Stock { get; set; }
}
