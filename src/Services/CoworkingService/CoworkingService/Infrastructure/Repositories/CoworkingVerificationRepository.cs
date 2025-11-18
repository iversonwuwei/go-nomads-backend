using System;
using CoworkingService.Domain.Entities;
using CoworkingService.Domain.Repositories;
using Postgrest;
using Client = Supabase.Client;

namespace CoworkingService.Infrastructure.Repositories;

/// <summary>
///     Supabase 实现的 Coworking 认证仓储
/// </summary>
public class CoworkingVerificationRepository : ICoworkingVerificationRepository
{
    private readonly ILogger<CoworkingVerificationRepository> _logger;
    private readonly Client _supabaseClient;

    public CoworkingVerificationRepository(Client supabaseClient, ILogger<CoworkingVerificationRepository> logger)
    {
        _supabaseClient = supabaseClient;
        _logger = logger;
    }

    public async Task<CoworkingVerification> AddAsync(CoworkingVerification verification)
    {
        try
        {
            var response = await _supabaseClient
                .From<CoworkingVerification>()
                .Insert(verification);

            var created = response.Models.FirstOrDefault();
            if (created == null) throw new InvalidOperationException("创建认证记录失败");

            return created;
        }
        catch (Exception ex)
        {
            if (IsDuplicateVoteViolation(ex))
            {
                _logger.LogWarning(
                    ex,
                    "检测到重复认证: CoworkingId={CoworkingId}, UserId={UserId}",
                    verification.CoworkingId,
                    verification.UserId);

                throw new InvalidOperationException("您已经为该 Coworking 提交过认证", ex);
            }

            _logger.LogError(
                ex,
                "创建 Coworking 认证记录时发生错误: CoworkingId={CoworkingId}, UserId={UserId}",
                verification.CoworkingId,
                verification.UserId);

            throw;
        }
    }

    public async Task<bool> HasUserVerifiedAsync(Guid coworkingId, Guid userId)
    {
        var response = await _supabaseClient
            .From<CoworkingVerification>()
            .Where(x => x.CoworkingId == coworkingId && x.UserId == userId)
            .Get();

        return response.Models.Any();
    }

    public async Task<int> GetVerificationCountAsync(Guid coworkingId)
    {
        var response = await _supabaseClient
            .From<CoworkingVerification>()
            .Where(x => x.CoworkingId == coworkingId)
            .Get();

        return response.Models.Count;
    }

    public async Task<Dictionary<Guid, int>> GetCountsByCoworkingIdsAsync(IEnumerable<Guid> coworkingIds)
    {
        var ids = coworkingIds?.Where(id => id != Guid.Empty).Distinct().ToList() ?? new List<Guid>();
        if (ids.Count == 0) return new Dictionary<Guid, int>();

        var filterValues = ids.Select(id => id.ToString()).ToArray();
        var response = await _supabaseClient
            .From<CoworkingVerification>()
            .Filter("coworking_id", Postgrest.Constants.Operator.In, filterValues)
            .Get();

        return response.Models
            .GroupBy(v => v.CoworkingId)
            .ToDictionary(g => g.Key, g => g.Count());
    }

    private static bool IsDuplicateVoteViolation(Exception ex)
    {
        if (ex == null) return false;

        var message = ex.Message;
        if (string.IsNullOrWhiteSpace(message)) return false;

        return message.Contains("duplicate key value violates unique constraint", StringComparison.OrdinalIgnoreCase);
    }
}
