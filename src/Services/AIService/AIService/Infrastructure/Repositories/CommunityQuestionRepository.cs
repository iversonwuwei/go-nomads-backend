using AIService.Domain.Entities;
using AIService.Domain.Repositories;
using Client = Supabase.Client;

namespace AIService.Infrastructure.Repositories;

public class CommunityQuestionRepository : ICommunityQuestionRepository
{
    private readonly ILogger<CommunityQuestionRepository> _logger;
    private readonly Client _supabaseClient;

    public CommunityQuestionRepository(Client supabaseClient, ILogger<CommunityQuestionRepository> logger)
    {
        _supabaseClient = supabaseClient;
        _logger = logger;
    }

    public async Task<List<CommunityQuestion>> GetRecentAsync(string? city = null, int limit = 20)
    {
        try
        {
            var query = _supabaseClient
                .From<CommunityQuestion>()
                .Order(question => question.CreatedAt, Postgrest.Constants.Ordering.Descending)
                .Range(0, Math.Max(limit - 1, 0));

            if (!string.IsNullOrWhiteSpace(city))
                query = query.Where(question => question.City == city);

            var response = await query.Get();
            return response.Models ?? new List<CommunityQuestion>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取 Community questions 失败，City: {City}", city ?? "all");
            throw;
        }
    }

    public async Task<CommunityQuestion?> GetByIdAsync(Guid id)
    {
        try
        {
            return await _supabaseClient
                .From<CommunityQuestion>()
                .Where(question => question.Id == id)
                .Single();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "获取 Community question 失败，Id: {QuestionId}", id);
            return null;
        }
    }

    public async Task<CommunityQuestion> CreateAsync(CommunityQuestion question)
    {
        question.CreatedAt = DateTime.UtcNow;
        question.UpdatedAt = DateTime.UtcNow;

        var response = await _supabaseClient
            .From<CommunityQuestion>()
            .Insert(question);

        return response.Models.First();
    }

    public async Task<CommunityQuestion?> UpdateAsync(CommunityQuestion question)
    {
        question.UpdatedAt = DateTime.UtcNow;

        var response = await _supabaseClient
            .From<CommunityQuestion>()
            .Where(item => item.Id == question.Id)
            .Update(question);

        return response.Models.FirstOrDefault();
    }
}