using AIService.Domain.Entities;
using AIService.Domain.Repositories;
using Client = Supabase.Client;

namespace AIService.Infrastructure.Repositories;

public class CommunityVoteRepository : ICommunityVoteRepository
{
    private readonly Client _supabaseClient;

    public CommunityVoteRepository(Client supabaseClient)
    {
        _supabaseClient = supabaseClient;
    }

    public async Task<bool> HasQuestionVoteAsync(Guid questionId, Guid userId)
    {
        var response = await _supabaseClient
            .From<CommunityQuestionVote>()
            .Where(vote => vote.QuestionId == questionId && vote.UserId == userId)
            .Get();

        return response.Models.Count > 0;
    }

    public async Task<HashSet<Guid>> GetQuestionVoteIdsAsync(Guid userId, IEnumerable<Guid> questionIds)
    {
        var ids = questionIds.Distinct().ToList();
        if (ids.Count == 0)
            return new HashSet<Guid>();

        var response = await _supabaseClient
            .From<CommunityQuestionVote>()
            .Where(vote => vote.UserId == userId)
            .Get();

        return response.Models
            .Where(vote => ids.Contains(vote.QuestionId))
            .Select(vote => vote.QuestionId)
            .ToHashSet();
    }

    public async Task SetQuestionVoteAsync(Guid questionId, Guid userId, bool isUpvoted)
    {
        if (isUpvoted)
        {
            await _supabaseClient
                .From<CommunityQuestionVote>()
                .Insert(new CommunityQuestionVote
                {
                    QuestionId = questionId,
                    UserId = userId,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                });

            return;
        }

        await _supabaseClient
            .From<CommunityQuestionVote>()
            .Where(vote => vote.QuestionId == questionId && vote.UserId == userId)
            .Delete();
    }

    public async Task<bool> HasAnswerVoteAsync(Guid answerId, Guid userId)
    {
        var response = await _supabaseClient
            .From<CommunityAnswerVote>()
            .Where(vote => vote.AnswerId == answerId && vote.UserId == userId)
            .Get();

        return response.Models.Count > 0;
    }

    public async Task<HashSet<Guid>> GetAnswerVoteIdsAsync(Guid userId, IEnumerable<Guid> answerIds)
    {
        var ids = answerIds.Distinct().ToList();
        if (ids.Count == 0)
            return new HashSet<Guid>();

        var response = await _supabaseClient
            .From<CommunityAnswerVote>()
            .Where(vote => vote.UserId == userId)
            .Get();

        return response.Models
            .Where(vote => ids.Contains(vote.AnswerId))
            .Select(vote => vote.AnswerId)
            .ToHashSet();
    }

    public async Task SetAnswerVoteAsync(Guid answerId, Guid userId, bool isUpvoted)
    {
        if (isUpvoted)
        {
            await _supabaseClient
                .From<CommunityAnswerVote>()
                .Insert(new CommunityAnswerVote
                {
                    AnswerId = answerId,
                    UserId = userId,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                });

            return;
        }

        await _supabaseClient
            .From<CommunityAnswerVote>()
            .Where(vote => vote.AnswerId == answerId && vote.UserId == userId)
            .Delete();
    }
}