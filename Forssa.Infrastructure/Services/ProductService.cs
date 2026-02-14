using AutoMapper;
using Forssa.Application.DTOs;
using Forssa.Application.Interfaces;
using Forssa.Domain.Entities;

namespace Forssa.Infrastructure.Services;

// Example service that inherits from BaseService
public class ProductService : BaseService<Product, ProductDTO>
{
    public ProductService(IBaseRepository<Product> repository, IMapper mapper)
        : base(repository, mapper)
    {
    }

    // Add custom business logic methods here
    public async Task<IEnumerable<ProductDTO>> GetProductsByPriceRangeAsync(decimal minPrice, decimal maxPrice)
    {
        var products = await _repository.FindAsync(p => p.Price >= minPrice && p.Price <= maxPrice);
        return _mapper.Map<IEnumerable<ProductDTO>>(products);
    }

    public async Task<IEnumerable<ProductDTO>> GetProductsInStockAsync()
    {
        var products = await _repository.FindAsync(p => p.Stock > 0);
        return _mapper.Map<IEnumerable<ProductDTO>>(products);
    }
}
