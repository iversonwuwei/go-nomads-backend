using System.Text.Json;
using GoNomads.Shared.Models;
using Microsoft.AspNetCore.Mvc;
using UserService.Application.DTOs;
using UserService.Domain.Repositories;

namespace UserService.API.Controllers;

/// <summary>
///     法律文档 API — 隐私政策、用户协议等（数据从数据库获取）
/// </summary>
[ApiController]
[Route("api/v1/users/legal")]
public class LegalController : ControllerBase
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    private readonly ILegalDocumentRepository _legalDocumentRepository;
    private readonly ILogger<LegalController> _logger;

    public LegalController(
        ILegalDocumentRepository legalDocumentRepository,
        ILogger<LegalController> logger)
    {
        _legalDocumentRepository = legalDocumentRepository;
        _logger = logger;
    }

    /// <summary>
    ///     获取当前生效的法律文档摘要列表
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<ApiResponse<List<LegalDocumentSummaryDto>>>> GetCurrentDocumentsAsync(
        [FromQuery] string lang = "zh",
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("📚 获取当前法律文档摘要列表, lang={Lang}", lang);

        var documents = await _legalDocumentRepository.ListCurrentAsync(lang, cancellationToken);

        if (documents.Count == 0 && lang != "zh")
        {
            _logger.LogInformation("⚠️ 未找到 {Lang} 当前法律文档，回退到中文", lang);
            documents = await _legalDocumentRepository.ListCurrentAsync("zh", cancellationToken);
        }

        var data = documents
            .Select(document => new LegalDocumentSummaryDto
            {
                Id = document.Id,
                DocumentType = document.DocumentType,
                Version = document.Version,
                Language = document.Language,
                Title = document.Title,
                EffectiveDate = document.EffectiveDate,
                IsCurrent = document.IsCurrent
            })
            .ToList();

        return Ok(new ApiResponse<List<LegalDocumentSummaryDto>>
        {
            Success = true,
            Message = "获取成功",
            Data = data
        });
    }

    /// <summary>
    ///     获取隐私政策
    /// </summary>
    [HttpGet("privacy-policy")]
    public async Task<ActionResult<ApiResponse<LegalDocumentDto>>> GetPrivacyPolicyAsync(
        [FromQuery] string lang = "zh",
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("📄 获取隐私政策, lang={Lang}", lang);

        var response = await GetDocumentByTypeAsync("privacy-policy", lang, "隐私政策文档尚未配置", cancellationToken);
        return response;
    }

    /// <summary>
    ///     获取用户协议
    /// </summary>
    [HttpGet("terms-of-service")]
    public async Task<ActionResult<ApiResponse<LegalDocumentDto>>> GetTermsOfServiceAsync(
        [FromQuery] string lang = "zh",
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("📄 获取用户协议, lang={Lang}", lang);

        var response = await GetDocumentByTypeAsync("terms-of-service", lang, "用户协议文档尚未配置", cancellationToken);
        return response;
    }

    /// <summary>
    ///     查询法律文档历史版本列表（轻量元数据，不含全文内容）
    /// </summary>
    [HttpGet("history")]
    public async Task<ActionResult<ApiResponse<List<LegalDocumentVersionDto>>>> GetHistoryAsync(
        [FromQuery(Name = "type")] string documentType,
        [FromQuery] string lang = "zh",
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(documentType))
        {
            return BadRequest(new ApiResponse<List<LegalDocumentVersionDto>>
            {
                Success = false,
                Message = "缺少 type 参数"
            });
        }

        _logger.LogInformation("📚 获取法律文档历史版本, type={Type}, lang={Lang}", documentType, lang);

        var documents = await _legalDocumentRepository.ListByTypeAsync(documentType, lang, cancellationToken);

        if (documents.Count == 0 && lang != "zh")
        {
            _logger.LogInformation("⚠️ 未找到 {Lang} 历史版本，type={Type}，回退到中文", lang, documentType);
            documents = await _legalDocumentRepository.ListByTypeAsync(documentType, "zh", cancellationToken);
        }

        var data = documents.Select(document => new LegalDocumentVersionDto
        {
            Id = document.Id,
            DocumentType = document.DocumentType,
            Version = document.Version,
            Language = document.Language,
            Title = document.Title,
            EffectiveDate = document.EffectiveDate,
            IsCurrent = document.IsCurrent,
            CreatedAt = document.CreatedAt,
            UpdatedAt = document.UpdatedAt
        }).ToList();

        return Ok(new ApiResponse<List<LegalDocumentVersionDto>>
        {
            Success = true,
            Message = "获取成功",
            Data = data
        });
    }

    private async Task<ActionResult<ApiResponse<LegalDocumentDto>>> GetDocumentByTypeAsync(
        string documentType,
        string lang,
        string notFoundMessage,
        CancellationToken cancellationToken)
    {
        var document = await _legalDocumentRepository.GetCurrentAsync(documentType, lang, cancellationToken);

        if (document == null)
        {
            // 如果请求的语言没有，尝试回退到中文
            if (lang != "zh")
            {
                _logger.LogInformation("⚠️ 未找到 {Lang} 版本法律文档，type={Type}，回退到中文", lang, documentType);
                document = await _legalDocumentRepository.GetCurrentAsync(documentType, "zh", cancellationToken);
            }

            if (document == null)
            {
                return NotFound(new ApiResponse<LegalDocumentDto>
                {
                    Success = false,
                    Message = notFoundMessage
                });
            }
        }

        var dto = new LegalDocumentDto
        {
            Id = document.Id,
            DocumentType = document.DocumentType,
            Version = document.Version,
            Language = document.Language,
            Title = document.Title,
            EffectiveDate = document.EffectiveDate,
            IsCurrent = document.IsCurrent,
            Sections = DeserializeJson<List<LegalSectionDto>>(document.Sections) ?? new(),
            Summary = DeserializeJson<List<LegalSummaryDto>>(document.Summary) ?? new(),
            SdkList = DeserializeJson<List<SdkInfoDto>>(document.SdkList) ?? new()
        };

        return Ok(new ApiResponse<LegalDocumentDto>
        {
            Success = true,
            Message = "获取成功",
            Data = dto
        });
    }

    private static T? DeserializeJson<T>(string? json)
    {
        if (string.IsNullOrEmpty(json)) return default;
        try
        {
            return JsonSerializer.Deserialize<T>(json, JsonOptions);
        }
        catch
        {
            return default;
        }
    }
}
