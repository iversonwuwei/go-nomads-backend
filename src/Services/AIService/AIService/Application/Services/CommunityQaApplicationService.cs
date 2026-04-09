using System.Text.Json;
using AIService.Application.DTOs;
using AIService.Domain.Entities;
using AIService.Domain.Repositories;
using AIService.Infrastructure.GrpcClients;

namespace AIService.Application.Services;

public class CommunityQaApplicationService : ICommunityQaService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    private readonly ICommunityAnswerRepository _answerRepository;
    private readonly ILogger<CommunityQaApplicationService> _logger;
    private readonly ICommunityQuestionRepository _questionRepository;
    private readonly IUserGrpcClient _userGrpcClient;
    private readonly ICommunityVoteRepository _voteRepository;

    public CommunityQaApplicationService(
        ICommunityQuestionRepository questionRepository,
        ICommunityAnswerRepository answerRepository,
        ICommunityVoteRepository voteRepository,
        IUserGrpcClient userGrpcClient,
        ILogger<CommunityQaApplicationService> logger)
    {
        _questionRepository = questionRepository;
        _answerRepository = answerRepository;
        _voteRepository = voteRepository;
        _userGrpcClient = userGrpcClient;
        _logger = logger;
    }

    public async Task<List<CommunityQuestionResponse>> GetQuestionsForSnapshotAsync(Guid userId, string? city, int limit = 3)
    {
        var questions = await _questionRepository.GetRecentAsync(city, limit);
        if (questions.Count == 0 && !string.IsNullOrWhiteSpace(city))
            questions = await _questionRepository.GetRecentAsync(null, limit);

        return await MapQuestionsAsync(questions, userId);
    }

    public async Task<CommunityQuestionResponse> CreateQuestionAsync(Guid userId, CreateCommunityQuestionRequest request)
    {
        ValidateQuestionRequest(request);

        var question = new CommunityQuestion
        {
            UserId = userId,
            City = request.City.Trim(),
            Title = request.Title.Trim(),
            Content = request.Content.Trim(),
            TagsJson = JsonSerializer.Serialize(
                request.Tags.Where(tag => !string.IsNullOrWhiteSpace(tag)).Select(tag => tag.Trim()).Distinct(StringComparer.OrdinalIgnoreCase),
                JsonOptions),
            Upvotes = 0,
            AnswerCount = 0
        };

        var created = await _questionRepository.CreateAsync(question);
        _logger.LogInformation("Community question created. UserId: {UserId}, QuestionId: {QuestionId}", userId, created.Id);

        return await MapQuestionAsync(created, new List<CommunityAnswer>(), userId, new HashSet<Guid>());
    }

    public async Task<CommunityAnswerResponse> CreateAnswerAsync(Guid userId, Guid questionId, CreateCommunityAnswerRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Content))
            throw new ArgumentException("回答内容不能为空", nameof(request));

        var question = await _questionRepository.GetByIdAsync(questionId)
                       ?? throw new KeyNotFoundException("问题不存在");

        var answer = new CommunityAnswer
        {
            QuestionId = questionId,
            UserId = userId,
            Content = request.Content.Trim(),
            Upvotes = 0,
            IsAccepted = false
        };

        var created = await _answerRepository.CreateAsync(answer);
        question.AnswerCount += 1;
        await _questionRepository.UpdateAsync(question);

        _logger.LogInformation("Community answer created. UserId: {UserId}, QuestionId: {QuestionId}, AnswerId: {AnswerId}",
            userId,
            questionId,
            created.Id);

        return await MapAnswerAsync(created, userId, false);
    }

    public async Task<CommunityQuestionResponse> ToggleQuestionUpvoteAsync(Guid userId, Guid questionId)
    {
        var question = await _questionRepository.GetByIdAsync(questionId)
                       ?? throw new KeyNotFoundException("问题不存在");

        var hadVote = await _voteRepository.HasQuestionVoteAsync(questionId, userId);
        var nextVoteState = !hadVote;
        await _voteRepository.SetQuestionVoteAsync(questionId, userId, nextVoteState);

        question.Upvotes = Math.Max(0, question.Upvotes + (nextVoteState ? 1 : -1));
        var updated = await _questionRepository.UpdateAsync(question) ?? question;
        var answers = await _answerRepository.GetByQuestionIdsAsync(new[] { questionId });

        _logger.LogInformation("Community question upvote toggled. UserId: {UserId}, QuestionId: {QuestionId}, IsUpvoted: {IsUpvoted}",
            userId,
            questionId,
            nextVoteState);

        return await MapQuestionAsync(updated, answers, userId, new HashSet<Guid> { questionId });
    }

    public async Task<CommunityAnswerResponse> ToggleAnswerUpvoteAsync(Guid userId, Guid answerId)
    {
        var answer = await _answerRepository.GetByIdAsync(answerId)
                     ?? throw new KeyNotFoundException("答案不存在");

        var hadVote = await _voteRepository.HasAnswerVoteAsync(answerId, userId);
        var nextVoteState = !hadVote;
        await _voteRepository.SetAnswerVoteAsync(answerId, userId, nextVoteState);

        answer.Upvotes = Math.Max(0, answer.Upvotes + (nextVoteState ? 1 : -1));
        var updated = await _answerRepository.UpdateAsync(answer) ?? answer;

        _logger.LogInformation("Community answer upvote toggled. UserId: {UserId}, AnswerId: {AnswerId}, IsUpvoted: {IsUpvoted}",
            userId,
            answerId,
            nextVoteState);

        return await MapAnswerAsync(updated, userId, nextVoteState);
    }

    private async Task<List<CommunityQuestionResponse>> MapQuestionsAsync(List<CommunityQuestion> questions, Guid userId)
    {
        if (questions.Count == 0)
            return new List<CommunityQuestionResponse>();

        var questionIds = questions.Select(question => question.Id).ToList();
        var answers = await _answerRepository.GetByQuestionIdsAsync(questionIds);
        var answersByQuestionId = answers.GroupBy(answer => answer.QuestionId)
            .ToDictionary(group => group.Key, group => group.OrderBy(item => item.CreatedAt).ToList());

        var userIds = questions.Select(question => question.UserId)
            .Concat(answers.Select(answer => answer.UserId))
            .Distinct()
            .ToList();
        var users = await _userGrpcClient.GetUsersInfoByIdsAsync(userIds);
        var questionVoteIds = await _voteRepository.GetQuestionVoteIdsAsync(userId, questionIds);
        var answerVoteIds = await _voteRepository.GetAnswerVoteIdsAsync(userId, answers.Select(answer => answer.Id));

        return questions.Select(question => MapQuestion(
                question,
                answersByQuestionId.GetValueOrDefault(question.Id) ?? new List<CommunityAnswer>(),
                users,
                questionVoteIds.Contains(question.Id),
                answerVoteIds))
            .ToList();
    }

    private async Task<CommunityQuestionResponse> MapQuestionAsync(
        CommunityQuestion question,
        List<CommunityAnswer> answers,
        Guid userId,
        HashSet<Guid> questionVoteIds)
    {
        var userIds = new[] { question.UserId }.Concat(answers.Select(answer => answer.UserId)).Distinct().ToList();
        var users = await _userGrpcClient.GetUsersInfoByIdsAsync(userIds);
        var answerVoteIds = await _voteRepository.GetAnswerVoteIdsAsync(userId, answers.Select(answer => answer.Id));

        return MapQuestion(
            question,
            answers,
            users,
            questionVoteIds.Contains(question.Id),
            answerVoteIds);
    }

    private CommunityQuestionResponse MapQuestion(
        CommunityQuestion question,
        List<CommunityAnswer> answers,
        IReadOnlyDictionary<Guid, UserInfo> users,
        bool isUpvoted,
        IReadOnlySet<Guid> answerVoteIds)
    {
        var questionUser = users.GetValueOrDefault(question.UserId);
        var mappedAnswers = answers.Select(answer => MapAnswer(answer, users.GetValueOrDefault(answer.UserId), answerVoteIds.Contains(answer.Id))).ToList();

        return new CommunityQuestionResponse
        {
            Id = question.Id.ToString(),
            UserId = question.UserId.ToString(),
            UserName = ResolveUserName(question.UserId, questionUser),
            UserAvatar = questionUser?.Avatar,
            City = question.City,
            Title = question.Title,
            Content = question.Content,
            Tags = DeserializeTags(question.TagsJson),
            Upvotes = question.Upvotes,
            AnswerCount = mappedAnswers.Count,
            HasAcceptedAnswer = mappedAnswers.Any(answer => answer.IsAccepted),
            CreatedAt = question.CreatedAt,
            IsUpvoted = isUpvoted,
            Answers = mappedAnswers
        };
    }

    private async Task<CommunityAnswerResponse> MapAnswerAsync(CommunityAnswer answer, Guid userId, bool? isUpvotedOverride)
    {
        var user = await _userGrpcClient.GetUserInfoAsync(answer.UserId);
        var isUpvoted = isUpvotedOverride ?? await _voteRepository.HasAnswerVoteAsync(answer.Id, userId);
        return MapAnswer(answer, user, isUpvoted);
    }

    private static CommunityAnswerResponse MapAnswer(CommunityAnswer answer, UserInfo? user, bool isUpvoted)
    {
        return new CommunityAnswerResponse
        {
            Id = answer.Id.ToString(),
            QuestionId = answer.QuestionId.ToString(),
            UserId = answer.UserId.ToString(),
            UserName = ResolveUserName(answer.UserId, user),
            UserAvatar = user?.Avatar,
            Content = answer.Content,
            Upvotes = answer.Upvotes,
            IsAccepted = answer.IsAccepted,
            CreatedAt = answer.CreatedAt,
            IsUpvoted = isUpvoted
        };
    }

    private static List<string> DeserializeTags(string tagsJson)
    {
        if (string.IsNullOrWhiteSpace(tagsJson))
            return new List<string>();

        try
        {
            return JsonSerializer.Deserialize<List<string>>(tagsJson, JsonOptions) ?? new List<string>();
        }
        catch
        {
            return new List<string>();
        }
    }

    private static string ResolveUserName(Guid userId, UserInfo? user)
    {
        if (!string.IsNullOrWhiteSpace(user?.Name))
            return user!.Name;

        return $"Nomad {userId.ToString()[..8]}";
    }

    private static void ValidateQuestionRequest(CreateCommunityQuestionRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.City))
            throw new ArgumentException("城市不能为空", nameof(request));

        if (string.IsNullOrWhiteSpace(request.Title))
            throw new ArgumentException("标题不能为空", nameof(request));

        if (string.IsNullOrWhiteSpace(request.Content))
            throw new ArgumentException("内容不能为空", nameof(request));
    }
}