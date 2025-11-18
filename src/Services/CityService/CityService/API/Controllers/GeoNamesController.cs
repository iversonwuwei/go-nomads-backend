using CityService.Application.DTOs;
using CityService.Application.Services;
using GoNomads.Shared.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CityService.API.Controllers;

/// <summary>
///     GeoNames 数据导入管理
/// </summary>
[ApiController]
[Route("api/[controller]")]
[AllowAnonymous] // 允许匿名访问
public class GeoNamesController : ControllerBase
{
    private readonly IGeoNamesImportService _geoNamesService;
    private readonly ILogger<GeoNamesController> _logger;

    public GeoNamesController(
        IGeoNamesImportService geoNamesService,
        ILogger<GeoNamesController> logger)
    {
        _geoNamesService = geoNamesService;
        _logger = logger;
    }

    /// <summary>
    ///     从 GeoNames 导入城市数据
    /// </summary>
    /// <param name="options">导入选项</param>
    [HttpPost("import")]
    public async Task<ActionResult<ApiResponse<GeoNamesImportResult>>> ImportCities(
        [FromBody] GeoNamesImportOptions options)
    {
        try
        {
            _logger.LogInformation("开始导入 GeoNames 城市数据");

            var result = await _geoNamesService.ImportCitiesAsync(options);

            return Ok(new ApiResponse<GeoNamesImportResult>
            {
                Success = true,
                Message = $"导入完成。成功: {result.SuccessCount}, 跳过: {result.SkippedCount}, 失败: {result.FailedCount}",
                Data = result
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "导入 GeoNames 城市数据失败");
            return StatusCode(500, new ApiResponse<GeoNamesImportResult>
            {
                Success = false,
                Message = "导入失败",
                Errors = new List<string> { ex.Message }
            });
        }
    }

    /// <summary>
    ///     搜索 GeoNames 城市 (预览)
    /// </summary>
    /// <param name="query">搜索关键词</param>
    /// <param name="maxRows">最大返回数量</param>
    [HttpGet("search")]
    [AllowAnonymous] // 允许匿名访问预览功能
    public async Task<ActionResult<ApiResponse<List<GeoNamesCity>>>> SearchCities(
        [FromQuery] string query,
        [FromQuery] int maxRows = 10)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(query))
                return BadRequest(new ApiResponse<List<GeoNamesCity>>
                {
                    Success = false,
                    Message = "搜索关键词不能为空"
                });

            var cities = await _geoNamesService.SearchCitiesAsync(query, maxRows);

            return Ok(new ApiResponse<List<GeoNamesCity>>
            {
                Success = true,
                Message = $"找到 {cities.Count} 个城市",
                Data = cities
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "搜索 GeoNames 城市失败: {Query}", query);
            return StatusCode(500, new ApiResponse<List<GeoNamesCity>>
            {
                Success = false,
                Message = "搜索失败",
                Errors = new List<string> { ex.Message }
            });
        }
    }

    /// <summary>
    ///     根据城市名和国家获取 GeoNames 信息
    /// </summary>
    [HttpGet("city/{cityName}")]
    [AllowAnonymous]
    public async Task<ActionResult<ApiResponse<GeoNamesCity>>> GetCity(
        string cityName,
        [FromQuery] string countryCode = "US")
    {
        try
        {
            var city = await _geoNamesService.GetCityByNameAsync(cityName, countryCode);

            if (city == null)
                return NotFound(new ApiResponse<GeoNamesCity>
                {
                    Success = false,
                    Message = $"未找到城市: {cityName}"
                });

            return Ok(new ApiResponse<GeoNamesCity>
            {
                Success = true,
                Message = "查询成功",
                Data = city
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取城市信息失败: {CityName}", cityName);
            return StatusCode(500, new ApiResponse<GeoNamesCity>
            {
                Success = false,
                Message = "查询失败",
                Errors = new List<string> { ex.Message }
            });
        }
    }

    /// <summary>
    ///     批量导入特定国家的城市
    /// </summary>
    [HttpPost("import/country/{countryCode}")]
    public async Task<ActionResult<ApiResponse<GeoNamesImportResult>>> ImportCountryCities(
        string countryCode,
        [FromQuery] int minPopulation = 100000)
    {
        try
        {
            _logger.LogInformation("开始导入 {CountryCode} 的城市数据", countryCode);

            var options = new GeoNamesImportOptions
            {
                CountryCodes = new List<string> { countryCode.ToUpper() },
                MinPopulation = minPopulation,
                OverwriteExisting = false
            };

            var result = await _geoNamesService.ImportCitiesAsync(options);

            return Ok(new ApiResponse<GeoNamesImportResult>
            {
                Success = true,
                Message = $"导入完成。成功: {result.SuccessCount}, 跳过: {result.SkippedCount}, 失败: {result.FailedCount}",
                Data = result
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "导入 {CountryCode} 城市数据失败", countryCode);
            return StatusCode(500, new ApiResponse<GeoNamesImportResult>
            {
                Success = false,
                Message = "导入失败",
                Errors = new List<string> { ex.Message }
            });
        }
    }
}