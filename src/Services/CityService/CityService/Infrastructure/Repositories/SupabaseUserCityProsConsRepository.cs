using CityService.Domain.Entities;
using CityService.Domain.Repositories;
using Postgrest;
using Shared.Repositories;
using Client = Supabase.Client;

namespace CityService.Infrastructure.Repositories;

/// <summary>
///     基于 Supabase 的城市 Pros & Cons 仓储实现
/// </summary>
public class SupabaseUserCityProsConsRepository : SupabaseRepositoryBase<CityProsCons>, IUserCityProsConsRepository
{
    public SupabaseUserCityProsConsRepository(Client supabaseClient, ILogger<SupabaseUserCityProsConsRepository> logger)
        : base(supabaseClient, logger)
    {
    }

    public async Task<CityProsCons> AddAsync(CityProsCons prosCons)
    {
        prosCons.CreatedAt = DateTime.UtcNow;
        prosCons.UpdatedAt = DateTime.UtcNow;

        var response = await SupabaseClient
            .From<CityProsCons>()
            .Insert(prosCons);

        return response.Models.First();
    }

    public async Task<List<CityProsCons>> GetByCityIdAsync(string cityId, bool? isPro = null)
    {
        var query = SupabaseClient
            .From<CityProsCons>()
            .Where(x => x.CityId == cityId)
            .Filter("is_deleted", Constants.Operator.NotEqual, "true");

        if (isPro.HasValue) query = query.Where(x => x.IsPro == isPro.Value);

        var response = await query
            .Order(x => x.CreatedAt, Constants.Ordering.Descending)
            .Get();

        return response.Models;
    }

    public async Task<CityProsCons?> GetByIdAsync(Guid id)
    {
        try
        {
            var response = await SupabaseClient
                .From<CityProsCons>()
                .Where(x => x.Id == id)
                .Single();

            return response;
        }
        catch
        {
            return null;
        }
    }

    public async Task<CityProsCons> UpdateAsync(CityProsCons prosCons)
    {
        prosCons.UpdatedAt = DateTime.UtcNow;

        var response = await SupabaseClient
            .From<CityProsCons>()
            .Where(x => x.Id == prosCons.Id)
            .Update(prosCons);

        return response.Models.First();
    }

    public async Task<bool> DeleteAsync(Guid id, Guid userId)
    {
        try
        {
            // 逻辑删除：设置 IsDeleted = true
            var prosCons = await GetByIdAsync(id);
            if (prosCons == null || prosCons.UserId != userId) return false;

            prosCons.IsDeleted = true;
            prosCons.UpdatedAt = DateTime.UtcNow;

            await SupabaseClient
                .From<CityProsCons>()
                .Where(x => x.Id == id)
                .Update(prosCons);

            return true;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "删除 Pros & Cons 失败: {Id}, {UserId}", id, userId);
            return false;
        }
    }

    public async Task<CityProsConsVote?> GetUserVoteAsync(Guid prosConsId, Guid userId)
    {
        try
        {
            var response = await SupabaseClient
                .From<CityProsConsVote>()
                .Where(x => x.ProsConsId == prosConsId)
                .Where(x => x.VoterUserId == userId)
                .Single();

            return response;
        }
        catch
        {
            return null;
        }
    }

    public async Task<CityProsConsVote> AddVoteAsync(CityProsConsVote vote)
    {
        vote.CreatedAt = DateTime.UtcNow;

        var response = await SupabaseClient
            .From<CityProsConsVote>()
            .Insert(vote);

        return response.Models.First();
    }

    public async Task<bool> DeleteVoteAsync(Guid voteId)
    {
        try
        {
            await SupabaseClient
                .From<CityProsConsVote>()
                .Where(x => x.Id == voteId)
                .Delete();

            return true;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "删除投票失败: {VoteId}", voteId);
            return false;
        }
    }

    public async Task<CityProsConsVote> UpdateVoteAsync(CityProsConsVote vote)
    {
        var response = await SupabaseClient
            .From<CityProsConsVote>()
            .Where(x => x.Id == vote.Id)
            .Update(vote);

        return response.Models.First();
    }
}