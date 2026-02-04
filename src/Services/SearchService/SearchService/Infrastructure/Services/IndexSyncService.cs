using System.Diagnostics;
using Microsoft.Extensions.Options;
using SearchService.Application.Interfaces;
using SearchService.Domain.Models;
using SearchService.Infrastructure.Configuration;

namespace SearchService.Infrastructure.Services;

/// <summary>
/// 索引同步服务实现
/// </summary>
public class IndexSyncService : IIndexSyncService
{
    private readonly IElasticsearchService _elasticsearchService;
    private readonly ICityServiceClient _cityServiceClient;
    private readonly ICoworkingServiceClient _coworkingServiceClient;
    private readonly IndexSettings _indexSettings;
    private readonly ILogger<IndexSyncService> _logger;

    public IndexSyncService(
        IElasticsearchService elasticsearchService,
        ICityServiceClient cityServiceClient,
        ICoworkingServiceClient coworkingServiceClient,
        IOptions<IndexSettings> indexSettings,
        ILogger<IndexSyncService> logger)
    {
        _elasticsearchService = elasticsearchService;
        _cityServiceClient = cityServiceClient;
        _coworkingServiceClient = coworkingServiceClient;
        _indexSettings = indexSettings.Value;
        _logger = logger;
    }

    public async Task<SyncResult> SyncAllCitiesAsync()
    {
        var sw = Stopwatch.StartNew();
        var result = new SyncResult();

        try
        {
            _logger.LogInformation("开始同步城市数据到Elasticsearch...");

            // 确保索引存在
            await _elasticsearchService.CreateIndexIfNotExistsAsync<CitySearchDocument>(_indexSettings.CityIndex);

            // 获取所有城市数据
            var cities = await _cityServiceClient.GetAllCitiesAsync();

            if (!cities.Any())
            {
                _logger.LogWarning("没有找到需要同步的城市数据");
                result.Success = true;
                result.SuccessCount = 0;
                return result;
            }

            // 批量索引
            result.SuccessCount = await _elasticsearchService.BulkIndexAsync(
                _indexSettings.CityIndex,
                cities,
                c => c.Id.ToString()
            );

            result.FailedCount = cities.Count - result.SuccessCount;
            result.Success = result.FailedCount == 0;

            sw.Stop();
            result.ElapsedMilliseconds = sw.ElapsedMilliseconds;

            _logger.LogInformation("城市数据同步完成: 成功 {SuccessCount}, 失败 {FailedCount}, 耗时 {Elapsed}ms",
                result.SuccessCount, result.FailedCount, result.ElapsedMilliseconds);
        }
        catch (Exception ex)
        {
            sw.Stop();
            result.Success = false;
            result.ErrorMessage = ex.Message;
            result.ElapsedMilliseconds = sw.ElapsedMilliseconds;
            _logger.LogError(ex, "同步城市数据时发生异常");
        }

        return result;
    }

    public async Task<bool> SyncCityAsync(Guid cityId)
    {
        try
        {
            _logger.LogInformation("同步城市 {CityId} 到Elasticsearch...", cityId);

            var city = await _cityServiceClient.GetCityByIdAsync(cityId);
            if (city == null)
            {
                _logger.LogWarning("未找到城市 {CityId}", cityId);
                return false;
            }

            return await _elasticsearchService.IndexDocumentAsync(
                _indexSettings.CityIndex,
                cityId.ToString(),
                city
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "同步城市 {CityId} 时发生异常", cityId);
            return false;
        }
    }

    public async Task<bool> DeleteCityAsync(Guid cityId)
    {
        try
        {
            _logger.LogInformation("从Elasticsearch删除城市 {CityId}...", cityId);
            return await _elasticsearchService.DeleteDocumentAsync(_indexSettings.CityIndex, cityId.ToString());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "删除城市 {CityId} 时发生异常", cityId);
            return false;
        }
    }

    public async Task<SyncResult> SyncAllCoworkingsAsync()
    {
        var sw = Stopwatch.StartNew();
        var result = new SyncResult();

        try
        {
            _logger.LogInformation("开始同步共享办公空间数据到Elasticsearch...");

            // 确保索引存在
            await _elasticsearchService.CreateIndexIfNotExistsAsync<CoworkingSearchDocument>(_indexSettings.CoworkingIndex);

            // 获取所有共享办公空间数据
            var coworkings = await _coworkingServiceClient.GetAllCoworkingsAsync();

            if (!coworkings.Any())
            {
                _logger.LogWarning("没有找到需要同步的共享办公空间数据");
                result.Success = true;
                result.SuccessCount = 0;
                return result;
            }

            // 批量索引
            result.SuccessCount = await _elasticsearchService.BulkIndexAsync(
                _indexSettings.CoworkingIndex,
                coworkings,
                c => c.Id.ToString()
            );

            result.FailedCount = coworkings.Count - result.SuccessCount;
            result.Success = result.FailedCount == 0;

            sw.Stop();
            result.ElapsedMilliseconds = sw.ElapsedMilliseconds;

            _logger.LogInformation("共享办公空间数据同步完成: 成功 {SuccessCount}, 失败 {FailedCount}, 耗时 {Elapsed}ms",
                result.SuccessCount, result.FailedCount, result.ElapsedMilliseconds);
        }
        catch (Exception ex)
        {
            sw.Stop();
            result.Success = false;
            result.ErrorMessage = ex.Message;
            result.ElapsedMilliseconds = sw.ElapsedMilliseconds;
            _logger.LogError(ex, "同步共享办公空间数据时发生异常");
        }

        return result;
    }

    public async Task<bool> SyncCoworkingAsync(Guid coworkingId)
    {
        try
        {
            _logger.LogInformation("同步共享办公空间 {CoworkingId} 到Elasticsearch...", coworkingId);

            var coworking = await _coworkingServiceClient.GetCoworkingByIdAsync(coworkingId);
            if (coworking == null)
            {
                _logger.LogWarning("未找到共享办公空间 {CoworkingId}", coworkingId);
                return false;
            }

            return await _elasticsearchService.IndexDocumentAsync(
                _indexSettings.CoworkingIndex,
                coworkingId.ToString(),
                coworking
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "同步共享办公空间 {CoworkingId} 时发生异常", coworkingId);
            return false;
        }
    }

    public async Task<bool> DeleteCoworkingAsync(Guid coworkingId)
    {
        try
        {
            _logger.LogInformation("从Elasticsearch删除共享办公空间 {CoworkingId}...", coworkingId);
            return await _elasticsearchService.DeleteDocumentAsync(_indexSettings.CoworkingIndex, coworkingId.ToString());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "删除共享办公空间 {CoworkingId} 时发生异常", coworkingId);
            return false;
        }
    }

    public async Task<SyncResult> SyncAllAsync()
    {
        var sw = Stopwatch.StartNew();
        var result = new SyncResult
        {
            Details = new Dictionary<string, object>()
        };

        try
        {
            _logger.LogInformation("开始同步所有数据到Elasticsearch...");

            var citySyncResult = await SyncAllCitiesAsync();
            var coworkingSyncResult = await SyncAllCoworkingsAsync();

            result.SuccessCount = citySyncResult.SuccessCount + coworkingSyncResult.SuccessCount;
            result.FailedCount = citySyncResult.FailedCount + coworkingSyncResult.FailedCount;
            result.Success = citySyncResult.Success && coworkingSyncResult.Success;

            result.Details["cities"] = new
            {
                success = citySyncResult.SuccessCount,
                failed = citySyncResult.FailedCount,
                elapsed = citySyncResult.ElapsedMilliseconds
            };

            result.Details["coworkings"] = new
            {
                success = coworkingSyncResult.SuccessCount,
                failed = coworkingSyncResult.FailedCount,
                elapsed = coworkingSyncResult.ElapsedMilliseconds
            };

            sw.Stop();
            result.ElapsedMilliseconds = sw.ElapsedMilliseconds;

            _logger.LogInformation("所有数据同步完成: 成功 {SuccessCount}, 失败 {FailedCount}, 耗时 {Elapsed}ms",
                result.SuccessCount, result.FailedCount, result.ElapsedMilliseconds);
        }
        catch (Exception ex)
        {
            sw.Stop();
            result.Success = false;
            result.ErrorMessage = ex.Message;
            result.ElapsedMilliseconds = sw.ElapsedMilliseconds;
            _logger.LogError(ex, "同步所有数据时发生异常");
        }

        return result;
    }

    public async Task<SyncResult> RebuildAllIndexesAsync()
    {
        var sw = Stopwatch.StartNew();
        var result = new SyncResult
        {
            Details = new Dictionary<string, object>()
        };

        try
        {
            _logger.LogInformation("开始重建所有索引...");

            // 删除现有索引
            await _elasticsearchService.DeleteIndexAsync(_indexSettings.CityIndex);
            await _elasticsearchService.DeleteIndexAsync(_indexSettings.CoworkingIndex);

            // 重新创建索引
            await _elasticsearchService.CreateIndexIfNotExistsAsync<CitySearchDocument>(_indexSettings.CityIndex);
            await _elasticsearchService.CreateIndexIfNotExistsAsync<CoworkingSearchDocument>(_indexSettings.CoworkingIndex);

            // 同步所有数据
            result = await SyncAllAsync();

            sw.Stop();
            result.ElapsedMilliseconds = sw.ElapsedMilliseconds;

            _logger.LogInformation("索引重建完成, 耗时 {Elapsed}ms", result.ElapsedMilliseconds);
        }
        catch (Exception ex)
        {
            sw.Stop();
            result.Success = false;
            result.ErrorMessage = ex.Message;
            result.ElapsedMilliseconds = sw.ElapsedMilliseconds;
            _logger.LogError(ex, "重建索引时发生异常");
        }

        return result;
    }
}
