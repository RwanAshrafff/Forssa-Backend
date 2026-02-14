using Forssa.Application.DTOs;
using Forssa.Infrastructure.Services;
using Microsoft.AspNetCore.Mvc;

namespace Forssa.API.Controllers;

// Example controller that inherits from BaseController
[Route("api/[controller]")]
[ApiController]
public class ProductsController : BaseController
{
    private readonly ProductService _productService;

    public ProductsController(ProductService productService)
    {
        _productService = productService;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        try
        {
            var products = await _productService.GetAllAsync();
            return HandleResult(products, "Products retrieved successfully");
        }
        catch (Exception ex)
        {
            return InternalServerError(ex.Message);
        }
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        try
        {
            var product = await _productService.GetByIdAsync(id);
            return HandleResult(product, "Product retrieved successfully");
        }
        catch (Exception ex)
        {
            return InternalServerError(ex.Message);
        }
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] ProductDTO productDto)
    {
        try
        {
            var product = await _productService.CreateAsync(productDto);
            return Created(product);
        }
        catch (Exception ex)
        {
            return InternalServerError(ex.Message);
        }
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] ProductDTO productDto)
    {
        try
        {
            var product = await _productService.UpdateAsync(id, productDto);
            return HandleResult(product, "Product updated successfully");
        }
        catch (KeyNotFoundException ex)
        {
            return NotFoundResult(ex.Message);
        }
        catch (Exception ex)
        {
            return InternalServerError(ex.Message);
        }
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        try
        {
            var result = await _productService.SoftDeleteAsync(id);
            if (!result)
                return NotFoundResult("Product not found");

            return HandleResult(result, "Product deleted successfully");
        }
        catch (Exception ex)
        {
            return InternalServerError(ex.Message);
        }
    }
}
