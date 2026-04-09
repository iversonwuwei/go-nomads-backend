using AIService.Domain.Entities;

namespace AIService.Domain.Repositories;

public interface ICommunityAnswerRepository
{
    Task<List<CommunityAnswer>> GetByQuestionIdsAsync(IEnumerable<Guid> questionIds);

    Task<CommunityAnswer?> GetByIdAsync(Guid id);

    Task<CommunityAnswer> CreateAsync(CommunityAnswer answer);

    Task<CommunityAnswer?> UpdateAsync(CommunityAnswer answer);
}