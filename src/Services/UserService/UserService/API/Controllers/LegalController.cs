using System.Text.Json;
using GoNomads.Shared.Models;
using Microsoft.AspNetCore.Mvc;
using UserService.Application.DTOs;
using UserService.Domain.Repositories;

namespace UserService.API.Controllers;

/// <summary>
///     法律文档 API — 公开接口，无需认证
/// </summary>
[ApiController]
[Route("api/v1/users/legal")]
public class LegalController : ControllerBase
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true
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
    ///     获取当前生效的隐私政策
    /// </summary>
    [HttpGet("privacy-policy")]
    public async Task<ActionResult<ApiResponse<LegalDocumentDto>>> GetPrivacyPolicy(
        [FromQuery] string lang = "zh",
        [FromQuery] string? version = null,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("📋 获取隐私政策: lang={Lang}, version={Version}", lang, version ?? "current");

        try
        {
            var document = string.IsNullOrEmpty(version)
                ? await _legalDocumentRepository.GetCurrentAsync("privacy_policy", lang, cancellationToken)
                : await _legalDocumentRepository.GetByVersionAsync("privacy_policy", lang, version, cancellationToken);

            if (document == null)
            {
                return NotFound(new ApiResponse<LegalDocumentDto>
                {
                    Success = false,
                    Message = "未找到隐私政策文档"
                });
            }

            return Ok(new ApiResponse<LegalDocumentDto>
            {
                Success = true,
                Message = "Privacy policy retrieved successfully",
                Data = MapToDto(document)
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ 获取隐私政策失败");
            return StatusCode(500, new ApiResponse<LegalDocumentDto>
            {
                Success = false,
                Message = "获取隐私政策失败"
            });
        }
    }

    #region Private Methods

    private static LegalDocumentDto MapToDto(Domain.Entities.LegalDocument doc)
    {
        return new LegalDocumentDto
        {
            Id = doc.Id,
            DocumentType = doc.DocumentType,
            Version = doc.Version,
            Language = doc.Language,
            Title = doc.Title,
            EffectiveDate = doc.EffectiveDate,
            IsCurrent = doc.IsCurrent,
            Sections = DeserializeJson<List<LegalSectionDto>>(doc.Sections) ?? new(),
            Summary = DeserializeJson<List<LegalSummaryDto>>(doc.Summary) ?? new()
        };
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

    #endregion
}
