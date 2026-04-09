using AIService.Application.DTOs;

namespace AIService.Application.Services;

public interface ICommunityQaService
{
    Task<List<CommunityQuestionResponse>> GetQuestionsForSnapshotAsync(Guid userId, string? city, int limit = 3);

    Task<CommunityQuestionResponse> CreateQuestionAsync(Guid userId, CreateCommunityQuestionRequest request);

    Task<CommunityAnswerResponse> CreateAnswerAsync(Guid userId, Guid questionId, CreateCommunityAnswerRequest request);

    Task<CommunityQuestionResponse> ToggleQuestionUpvoteAsync(Guid userId, Guid questionId);

    Task<CommunityAnswerResponse> ToggleAnswerUpvoteAsync(Guid userId, Guid answerId);
}