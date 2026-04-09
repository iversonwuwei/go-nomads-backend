using AIService.Domain.Entities;
using AIService.Domain.Repositories;
using Client = Supabase.Client;

namespace AIService.Infrastructure.Repositories;

public class CommunityAnswerRepository : ICommunityAnswerRepository
{
    private readonly ILogger<CommunityAnswerRepository> _logger;
    private readonly Client _supabaseClient;

    public CommunityAnswerRepository(Client supabaseClient, ILogger<CommunityAnswerRepository> logger)
    {
        _supabaseClient = supabaseClient;
        _logger = logger;
    }

    public async Task<List<CommunityAnswer>> GetByQuestionIdsAsync(IEnumerable<Guid> questionIds)
    {
        var ids = questionIds.Distinct().ToList();
        if (ids.Count == 0)
            return new List<CommunityAnswer>();

        try
        {
            var response = await _supabaseClient
                .From<CommunityAnswer>()
                .Order(answer => answer.CreatedAt, Postgrest.Constants.Ordering.Ascending)
                .Get();

            return (response.Models ?? new List<CommunityAnswer>())
                .Where(answer => ids.Contains(answer.QuestionId))
                .ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取 Community answers 失败，QuestionCount: {Count}", ids.Count);
            throw;
        }
    }

    public async Task<CommunityAnswer?> GetByIdAsync(Guid id)
    {
        try
        {
            return await _supabaseClient
                .From<CommunityAnswer>()
                .Where(answer => answer.Id == id)
                .Single();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "获取 Community answer 失败，Id: {AnswerId}", id);
            return null;
        }
    }

    public async Task<CommunityAnswer> CreateAsync(CommunityAnswer answer)
    {
        answer.CreatedAt = DateTime.UtcNow;
        answer.UpdatedAt = DateTime.UtcNow;

        var response = await _supabaseClient
            .From<CommunityAnswer>()
            .Insert(answer);

        return response.Models.First();
    }

    public async Task<CommunityAnswer?> UpdateAsync(CommunityAnswer answer)
    {
        answer.UpdatedAt = DateTime.UtcNow;

        var response = await _supabaseClient
            .From<CommunityAnswer>()
            .Where(item => item.Id == answer.Id)
            .Update(answer);

        return response.Models.FirstOrDefault();
    }
}