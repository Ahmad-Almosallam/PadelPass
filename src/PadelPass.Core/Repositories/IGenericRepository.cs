using System.Data;
using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using PadelPass.Core.Entities;
using PadelPass.Core.Shared;

namespace PadelPass.Core.Repositories;

public interface IGenericRepository<TEntity> where TEntity : BaseEntity
{
    #region [ database transactional scope ]

    /// <summary>
    /// Creates an instance of the configured ExecutionStrategy.
    /// </summary>
    IExecutionStrategy CreateExecutionStrategy();

    /// <summary>
    /// Starts a new transaction with a given System.Data.IsolationLevel.
    /// </summary>
    IDbContextTransaction BeginTransaction(IsolationLevel isolationLevel = IsolationLevel.Unspecified);

    #endregion

    #region [ database query ]

    Task<TEntity?> GetByIdAsync(int id, CancellationToken cancellationToken = default);

    TEntity? GetById(int id);

    List<TEntity> GetList(Expression<Func<TEntity, bool>> expression);

    Task<List<TEntity>> GetListAsync(Expression<Func<TEntity, bool>> expression);

    PaginatedList<TEntity> GetPaginatedList(IQueryable<TEntity> query,
        int? pageNumber,
        int? pageSize,
        string orderBy,
        string orderType);

    Task<PaginatedList<TEntity>> GetPaginatedListAsync(IQueryable<TEntity> query,
        int? pageNumber,
        int? pageSize,
        string orderBy,
        string orderType,
        CancellationToken cancellationToken = default);

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
    IQueryable<TEntity> AsQueryable(bool trackable);


    bool Any(Expression<Func<TEntity, bool>> expression);
    Task<bool> AnyAsync(Expression<Func<TEntity, bool>> expression, CancellationToken cancellationToken);
    int Count(Expression<Func<TEntity, bool>> expression);
    Task<int> CountAsync(Expression<Func<TEntity, bool>> expression, CancellationToken cancellationToken = default);

    #endregion

    DbSet<TEntity> Table { get; }

    #region [ database actions ]

    /// <summary>
    /// don't use it, ever never. !!!!!
    /// </summary>
    /// <returns></returns>
    DbContext Context();

    /// <summary>
    /// Saves all changes made in this context to the database.
    /// </summary>
    int SaveChanges();

    /// <summary>
    /// Saves all changes made in this context to the database.
    /// </summary>
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);

    #endregion

    #region [ database context actions ]

    /// <summary>
    /// SaveChanges() or SaveChangesAsync() must be called to apply all database changes
    /// </summary>
    TEntity Insert(TEntity entity);

    /// <summary>
    /// SaveChanges() or SaveChangesAsync() must be called to apply all database changes
    /// </summary>
    void InsertRange(TEntity[] entities);

    /// <summary>
    /// SaveChanges() or SaveChangesAsync() must be called to apply all database changes
    /// </summary>
    void Update(TEntity entity);

    /// <summary>
    /// SaveChanges() or SaveChangesAsync() must be called to apply all database changes
    /// </summary>
    void UpdateRange(TEntity[] entities);

    /// <summary>
    /// SaveChanges() or SaveChangesAsync() must be called to apply all database changes
    /// </summary>
    void Delete(TEntity entity);

    /// <summary>
    /// SaveChanges() or SaveChangesAsync() must be called to apply all database changes
    /// </summary>
    void DeleteById(int id);

    /// <summary>
    /// SaveChanges() or SaveChangesAsync() must be called to apply all database changes
    /// </summary>
    void DeleteByQuery(Expression<Func<TEntity, bool>> predicate);


    #endregion
}