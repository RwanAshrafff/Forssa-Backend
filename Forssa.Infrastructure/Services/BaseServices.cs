using System.Linq.Expressions;
using AutoMapper;
using Forssa.Application.DTOs;
using Forssa.Application.Interfaces;
using Forssa.Domain.Entities;

namespace Forssa.Infrastructure.Services;

public abstract class BaseService<TEntity, TDto> : IBaseService<TEntity, TDto>
    where TEntity : BaseEntity
    where TDto : BaseDTO
{
    protected readonly IBaseRepository<TEntity> _repository;
    protected readonly IMapper _mapper;

    protected BaseService(IBaseRepository<TEntity> repository, IMapper mapper)
    {
        _repository = repository;
        _mapper = mapper;
    }

    public virtual async Task<TDto?> GetByIdAsync(Guid id)
    {
        var entity = await _repository.GetByIdAsync(id);
        return entity == null ? null : _mapper.Map<TDto>(entity);
    }

    public virtual async Task<IEnumerable<TDto>> GetAllAsync()
    {
        var entities = await _repository.GetAllAsync();
        return _mapper.Map<IEnumerable<TDto>>(entities);
    }

    public virtual async Task<IEnumerable<TDto>> FindAsync(Expression<Func<TEntity, bool>> predicate)
    {
        var entities = await _repository.FindAsync(predicate);
        return _mapper.Map<IEnumerable<TDto>>(entities);
    }

    public virtual async Task<TDto> CreateAsync(TDto dto)
    {
        var entity = _mapper.Map<TEntity>(dto);
        var createdEntity = await _repository.AddAsync(entity);
        return _mapper.Map<TDto>(createdEntity);
    }

    public virtual async Task<TDto> UpdateAsync(Guid id, TDto dto)
    {
        var existingEntity = await _repository.GetByIdAsync(id);
        if (existingEntity == null)
            throw new KeyNotFoundException($"Entity with id {id} not found");

        dto.Id = id;
        var entity = _mapper.Map<TEntity>(dto);
        entity.CreatedAt = existingEntity.CreatedAt;
        
        var updatedEntity = await _repository.UpdateAsync(entity);
        return _mapper.Map<TDto>(updatedEntity);
    }

    public virtual async Task<bool> DeleteAsync(Guid id)
    {
        return await _repository.DeleteAsync(id);
    }

    public virtual async Task<bool> SoftDeleteAsync(Guid id)
    {
        return await _repository.SoftDeleteAsync(id);
    }

    public virtual async Task<int> CountAsync()
    {
        return await _repository.CountAsync();
    }

    public virtual async Task<bool> ExistsAsync(Guid id)
    {
        return await _repository.ExistsAsync(id);
    }
}
