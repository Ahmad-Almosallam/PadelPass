using System.Data;
using System.Linq.Expressions;
using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using PadelPass.Core.Entities;
using PadelPass.Core.Shared;
using System.Linq.Dynamic.Core;
using PadelPass.Core.Repositories;

namespace PadelPass.Infrastructure.Repositories;

public class GenericRepository<TEntity> : IGenericRepository<TEntity> where TEntity : BaseEntity 
{
    #region [ injected variables ]

    private readonly PadelPassDbContext _context;
    private readonly DbSet<TEntity> _table;
    private readonly IMapper _mapper;

    public DbSet<TEntity> Table => _table;

    #endregion

    #region [ constructor ]

    public GenericRepository(PadelPassDbContext context, IMapper mapper)
    {
        _context = context;
        _table = _context.Set<TEntity>();
        _mapper = mapper;
    }

    #endregion

    #region [ database transactional scope ]

    /// <summary>
    /// Creates an instance of the configured ExecutionStrategy.
    /// </summary>
    public IExecutionStrategy CreateExecutionStrategy()
        => _context.Database.CreateExecutionStrategy();

    public IDbContextTransaction BeginTransaction(
        System.Transactions.IsolationLevel isolationLevel = System.Transactions.IsolationLevel.Unspecified)
    {
        throw new NotImplementedException();
    }

    /// <summary>
    /// Starts a new transaction with a given System.Data.IsolationLevel.
    /// </summary>
    public IDbContextTransaction BeginTransaction(IsolationLevel isolationLevel = IsolationLevel.Unspecified)
        => _context.Database.BeginTransaction(isolationLevel);

    #endregion

    #region [ database query ]

    /// <summary>
    /// Returns this object typed as IQueryable of TEntity
    /// </summary>
    /// <param name="trackable">
    /// If the value is false, The change tracker will not track any of the entities that are returned from a LINQ query.
    /// Otherwise, The change tracker will keep track of changes for all entities that are returned.
    /// 
    /// Keep notice that if trackable = true will degrade the performance sometimes
    /// </param>
    /// <returns></returns>
    public IQueryable<TEntity> AsQueryable(bool trackable) =>
        trackable ?
        _table.AsTracking()
        :
        _table.AsNoTracking();

    public TEntity? GetById(int id)
        => _table.Find(id);

    public Task<TEntity?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
        => Task.FromResult(GetById(id));

    

    public Task<bool> AnyAsync(Expression<Func<TEntity, bool>> expression, CancellationToken cancellationToken = default)
        => _table.AsNoTracking().AnyAsync(expression, cancellationToken);

    public bool Any(Expression<Func<TEntity, bool>> expression)
        => _table.AsNoTracking().Any(expression);

    public int Count(Expression<Func<TEntity, bool>> expression)
        => _table.AsNoTracking().Count(expression);

    public Task<int> CountAsync(Expression<Func<TEntity, bool>> expression, CancellationToken cancellationToken = default)
        => _table.AsNoTracking().CountAsync(expression, cancellationToken);

    public List<TEntity> GetList(Expression<Func<TEntity, bool>> query)
        => AsQueryable(false).Where(query).ToList();

    public Task<List<TEntity>> GetListAsync(Expression<Func<TEntity, bool>> query)
        => AsQueryable(false).Where(query).ToListAsync();

    public async Task<PaginatedList<TEntity>> GetPaginatedListAsync(IQueryable<TEntity> query,
        int? pageNumber,
        int? pageSize,
        string orderBy,
        string orderType,
        CancellationToken cancellationToken = default)
    {
        var tableEntity = query ?? _table.AsNoTracking();
        int pageNumberCriteria = pageNumber != null ? pageNumber.Value : 1;
        int pageSizeCriteria = pageSize != null ? pageSize.Value : 30;

        if (!string.IsNullOrWhiteSpace(orderBy))
            tableEntity = tableEntity.OrderBy($"{orderBy} {orderType.ToUpper()}");

        var count = await tableEntity.TagWith("FORCE_LEGACY").CountAsync(cancellationToken);
        var items = await tableEntity
            .Skip((pageNumberCriteria - 1) * pageSizeCriteria)
            .Take(pageSizeCriteria)
            .AsSplitQuery()
            .ToListAsync(cancellationToken);

        return new PaginatedList<TEntity>(_mapper.Map<List<TEntity>>(items),
            count,
            pageNumberCriteria,
            pageSizeCriteria);
    }

    public PaginatedList<TEntity> GetPaginatedList(IQueryable<TEntity> query,
        int? pageNumber,
        int? pageSize,
        string orderBy,
        string orderType)
    {
        var tableEntity = query ?? _table.AsNoTracking();
        int pageNumberCriteria = pageNumber != null ? pageNumber.Value : 1;
        int pageSizeCriteria = pageSize != null ? pageSize.Value : 30;

        if (!string.IsNullOrWhiteSpace(orderBy))
            tableEntity = tableEntity.OrderBy($"{orderBy} {orderType.ToUpper()}");

        var count = tableEntity.Count();
        var items = tableEntity
            .Skip((pageNumberCriteria - 1) * pageSizeCriteria)
            .Take(pageSizeCriteria)
            .ToList();

        return new PaginatedList<TEntity>(_mapper.Map<List<TEntity>>(items),
            count,
            pageNumberCriteria,
            pageSizeCriteria);
    }

    #endregion

    #region [ database actions ]

    /// <summary>
    /// don't use it, ever never. !!!!!
    /// </summary>
    /// <returns></returns>
    public DbContext Context() => _context;

    /// <summary>
    /// Saves all changes made in this context to the database.
    /// </summary>
    public int SaveChanges() => _context.SaveChanges();

    /// <summary>
    /// Saves all changes made in this context to the database.
    /// </summary>
    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        => _context.SaveChangesAsync(cancellationToken);

    #endregion

    #region [ database context actions ]

    /// <summary>
    /// SaveChanges() or SaveChangesAsync() must be called to apply all database changes
    /// </summary>
    public TEntity Insert(TEntity entity)
        => _table.Add(entity).Entity;

    /// <summary>
    /// SaveChanges() or SaveChangesAsync() must be called to apply all database changes
    /// </summary>
    public void InsertRange(TEntity[] entities)
        => _table.AddRange(entities);

    /// <summary>
    /// SaveChanges() or SaveChangesAsync() must be called to apply all database changes
    /// </summary>
    public void Update(TEntity entity)
        => _table.Update(entity);

    /// <summary>
    /// SaveChanges() or SaveChangesAsync() must be called to apply all database changes
    /// </summary>
    public void UpdateRange(TEntity[] entities)
        => _table.UpdateRange(entities);

    /// <summary>
    /// SaveChanges() or SaveChangesAsync() must be called to apply all database changes
    /// </summary>
    public void DeleteById(int id)
    {
        var entity = GetById(id) ?? throw new NullReferenceException(nameof(TEntity));
        _table.Remove(entity);
    }
    

    /// <summary>
    /// SaveChanges() or SaveChangesAsync() must be called to apply all database changes
    /// </summary>
    public void DeleteByQuery(Expression<Func<TEntity, bool>> predicate)
    {
        var items = AsQueryable(true)
            .Where(predicate)
            .ToList();

        if (items is not null && items.Any())
            _table.RemoveRange(items);
    }

    public void Delete(TEntity entity)
    {
        if (entity is null)
            throw new NullReferenceException(nameof(TEntity));
        else
            _table.Remove(entity);
    }

    #endregion
}