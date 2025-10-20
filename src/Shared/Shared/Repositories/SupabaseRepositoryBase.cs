using Microsoft.Extensions.Logging;
using Postgrest.Models;
using Supabase;

namespace Shared.Repositories;

/// <summary>
/// Supabase Repository 基类
/// 提供通用的 CRUD 操作
/// </summary>
/// <typeparam name="T">实体类型，必须继承自 BaseModel</typeparam>
public abstract class SupabaseRepositoryBase<T> where T : BaseModel, new()
{
    protected readonly Client SupabaseClient;
    protected readonly ILogger Logger;

    protected SupabaseRepositoryBase(Client supabaseClient, ILogger logger)
    {
        SupabaseClient = supabaseClient ?? throw new ArgumentNullException(nameof(supabaseClient));
        Logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// 获取所有记录
    /// </summary>
    public virtual async Task<IEnumerable<T>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        Logger.LogInformation("Fetching all records of type {Type}", typeof(T).Name);

        try
        {
            var response = await SupabaseClient
                .From<T>()
                .Get(cancellationToken);

            Logger.LogInformation("Successfully fetched {Count} records", response.Models.Count);
            return response.Models;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error fetching all records of type {Type}", typeof(T).Name);
            throw;
        }
    }

    /// <summary>
    /// 获取分页记录
    /// </summary>
    public virtual async Task<(IEnumerable<T> Items, int TotalCount)> GetPagedAsync(
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        Logger.LogInformation("Fetching paged records - Page: {Page}, PageSize: {PageSize}", page, pageSize);

        try
        {
            var from = (page - 1) * pageSize;
            var to = from + pageSize - 1;

            // 获取总数
            var countResponse = await SupabaseClient
                .From<T>()
                .Get(cancellationToken);

            var totalCount = countResponse.Models.Count;

            // 获取分页数据
            var response = await SupabaseClient
                .From<T>()
                .Range(from, to)
                .Get(cancellationToken);

            Logger.LogInformation("Successfully fetched {Count} records", response.Models.Count);
            return (response.Models, totalCount);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error fetching paged records of type {Type}", typeof(T).Name);
            throw;
        }
    }

    /// <summary>
    /// 根据 ID 获取记录
    /// </summary>
    public virtual async Task<T?> GetByIdAsync(string id, string idColumn = "id", CancellationToken cancellationToken = default)
    {
        Logger.LogInformation("Fetching record by ID: {Id}", id);

        try
        {
            var allRecords = await SupabaseClient
                .From<T>()
                .Get(cancellationToken);

            // 使用反射查找匹配的记录（不区分大小写）
            var property = typeof(T).GetProperty(idColumn, System.Reflection.BindingFlags.IgnoreCase | 
                                                           System.Reflection.BindingFlags.Public | 
                                                           System.Reflection.BindingFlags.Instance);
            if (property == null)
            {
                throw new InvalidOperationException($"Property '{idColumn}' not found on type {typeof(T).Name}");
            }

            var match = allRecords.Models.FirstOrDefault(entity =>
            {
                var value = property.GetValue(entity)?.ToString();
                return value == id;
            });

            return match;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error fetching record by ID: {Id}", id);
            return null;
        }
    }

    /// <summary>
    /// 插入记录
    /// </summary>
    public virtual async Task<T> InsertAsync(T entity, CancellationToken cancellationToken = default)
    {
        Logger.LogInformation("Inserting record of type {Type}", typeof(T).Name);

        try
        {
            var response = await SupabaseClient
                .From<T>()
                .Insert(new List<T> { entity });

            var inserted = response.Models.FirstOrDefault();
            if (inserted == null)
            {
                throw new InvalidOperationException("Failed to insert record");
            }

            Logger.LogInformation("Successfully inserted record");
            return inserted;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error inserting record of type {Type}", typeof(T).Name);
            throw;
        }
    }

    /// <summary>
    /// 批量插入记录
    /// </summary>
    public virtual async Task<IEnumerable<T>> InsertManyAsync(IEnumerable<T> entities, CancellationToken cancellationToken = default)
    {
        Logger.LogInformation("Inserting {Count} records of type {Type}", entities.Count(), typeof(T).Name);

        try
        {
            var response = await SupabaseClient
                .From<T>()
                .Insert(entities.ToList());

            Logger.LogInformation("Successfully inserted {Count} records", response.Models.Count);
            return response.Models;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error inserting records of type {Type}", typeof(T).Name);
            throw;
        }
    }

    /// <summary>
    /// 删除记录
    /// </summary>
    public virtual async Task<bool> DeleteAsync(string id, string idColumn = "id", CancellationToken cancellationToken = default)
    {
        Logger.LogInformation("Deleting record by ID: {Id}", id);

        try
        {
            // 先获取记录确认存在
            var record = await GetByIdAsync(id, idColumn, cancellationToken);
            if (record == null)
            {
                Logger.LogWarning("Record not found for deletion: {Id}", id);
                return false;
            }

            // 使用 Get() 获取所有记录，然后筛选并删除
            await SupabaseClient
                .From<T>()
                .Delete();

            Logger.LogInformation("Successfully deleted record: {Id}", id);
            return true;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error deleting record: {Id}", id);
            return false;
        }
    }

    /// <summary>
    /// 检查记录是否存在
    /// </summary>
    public virtual async Task<bool> ExistsAsync(string id, string idColumn = "id", CancellationToken cancellationToken = default)
    {
        Logger.LogInformation("Checking if record exists: {Id}", id);

        try
        {
            var entity = await GetByIdAsync(id, idColumn, cancellationToken);
            return entity != null;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error checking record existence: {Id}", id);
            return false;
        }
    }
}
