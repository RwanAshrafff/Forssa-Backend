using System.Linq.Expressions;
using Forssa.Application.DTOs;
using Forssa.Domain.Entities;

namespace Forssa.Application.Interfaces;

public interface IBaseService<TEntity, TDto> 
    where TEntity : BaseEntity
    where TDto : BaseDTO
{
    Task<TDto?> GetByIdAsync(Guid id);
    Task<IEnumerable<TDto>> GetAllAsync();
    Task<IEnumerable<TDto>> FindAsync(Expression<Func<TEntity, bool>> predicate);
    Task<TDto> CreateAsync(TDto dto);
    Task<TDto> UpdateAsync(Guid id, TDto dto);
    Task<bool> DeleteAsync(Guid id);
    Task<bool> SoftDeleteAsync(Guid id);
    Task<int> CountAsync();
    Task<bool> ExistsAsync(Guid id);
}
