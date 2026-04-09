namespace AIService.Domain.Repositories;

public interface ICommunityVoteRepository
{
    Task<bool> HasQuestionVoteAsync(Guid questionId, Guid userId);

    Task<HashSet<Guid>> GetQuestionVoteIdsAsync(Guid userId, IEnumerable<Guid> questionIds);

    Task SetQuestionVoteAsync(Guid questionId, Guid userId, bool isUpvoted);

    Task<bool> HasAnswerVoteAsync(Guid answerId, Guid userId);

    Task<HashSet<Guid>> GetAnswerVoteIdsAsync(Guid userId, IEnumerable<Guid> answerIds);

    Task SetAnswerVoteAsync(Guid answerId, Guid userId, bool isUpvoted);
}