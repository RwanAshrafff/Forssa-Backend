using Forssa.Application.DTOs;

namespace Forssa.Application.DTOs;

// Example DTO that inherits from BaseDTO
public class ProductDTO : BaseDTO
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public int Stock { get; set; }
}
