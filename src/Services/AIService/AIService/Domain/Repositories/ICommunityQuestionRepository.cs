using AIService.Domain.Entities;

namespace AIService.Domain.Repositories;

public interface ICommunityQuestionRepository
{
    Task<List<CommunityQuestion>> GetRecentAsync(string? city = null, int limit = 20);

    Task<CommunityQuestion?> GetByIdAsync(Guid id);

    Task<CommunityQuestion> CreateAsync(CommunityQuestion question);

    Task<CommunityQuestion?> UpdateAsync(CommunityQuestion question);
}