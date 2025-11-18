using CityService.Domain.Entities;
using CityService.Domain.Repositories;
using Postgrest;
using Shared.Repositories;
using Client = Supabase.Client;

namespace CityService.Infrastructure.Repositories;

/// <summary>
///     基于 Supabase 的用户城市费用仓储实现
/// </summary>
public class SupabaseUserCityExpenseRepository : SupabaseRepositoryBase<UserCityExpense>, IUserCityExpenseRepository
{
    public SupabaseUserCityExpenseRepository(Client supabaseClient, ILogger<SupabaseUserCityExpenseRepository> logger)
        : base(supabaseClient, logger)
    {
    }

    public async Task<UserCityExpense> CreateAsync(UserCityExpense expense)
    {
        expense.CreatedAt = DateTime.UtcNow;
        var response = await SupabaseClient
            .From<UserCityExpense>()
            .Insert(expense);

        return response.Models.First();
    }

    public async Task<IEnumerable<UserCityExpense>> GetByCityIdAsync(string cityId)
    {
        var response = await SupabaseClient
            .From<UserCityExpense>()
            .Where(x => x.CityId == cityId)
            .Order(x => x.CreatedAt, Constants.Ordering.Descending)
            .Get();

        return response.Models;
    }

    public async Task<IEnumerable<UserCityExpense>> GetByCityIdAndUserIdAsync(string cityId, Guid userId)
    {
        var response = await SupabaseClient
            .From<UserCityExpense>()
            .Where(x => x.CityId == cityId && x.UserId == userId)
            .Order(x => x.CreatedAt, Constants.Ordering.Descending)
            .Get();

        return response.Models;
    }

    public async Task<IEnumerable<UserCityExpense>> GetByUserIdAsync(Guid userId)
    {
        var response = await SupabaseClient
            .From<UserCityExpense>()
            .Where(x => x.UserId == userId)
            .Order(x => x.CreatedAt, Constants.Ordering.Descending)
            .Get();

        return response.Models;
    }

    public async Task<UserCityExpense?> GetByIdAsync(Guid id)
    {
        try
        {
            var response = await SupabaseClient
                .From<UserCityExpense>()
                .Where(x => x.Id == id)
                .Single();

            return response;
        }
        catch
        {
            return null;
        }
    }

    public async Task<bool> DeleteAsync(Guid id, Guid userId)
    {
        try
        {
            await SupabaseClient
                .From<UserCityExpense>()
                .Where(x => x.Id == id && x.UserId == userId)
                .Delete();

            return true;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "删除费用失败: {ExpenseId}, {UserId}", id, userId);
            return false;
        }
    }
}