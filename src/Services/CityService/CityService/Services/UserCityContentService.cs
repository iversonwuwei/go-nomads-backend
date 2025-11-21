using System.Data;
using CityService.Application.DTOs;
using Npgsql;

namespace CityService.Services;

/// <summary>
///     用户城市内容服务（照片、费用、评论）
/// </summary>
public interface IUserCityContentService
{
    // 照片相关
    Task<UserCityPhotoDto> AddPhotoAsync(Guid userId, AddCityPhotoRequest request);
    Task<List<UserCityPhotoDto>> GetCityPhotosAsync(string cityId, Guid? userId = null);
    Task<List<UserCityPhotoDto>> GetUserPhotosAsync(Guid userId);
    Task<bool> DeletePhotoAsync(Guid userId, Guid photoId);

    // 费用相关
    Task<UserCityExpenseDto> AddExpenseAsync(Guid userId, AddCityExpenseRequest request);
    Task<List<UserCityExpenseDto>> GetCityExpensesAsync(string cityId, Guid? userId = null);
    Task<List<UserCityExpenseDto>> GetUserExpensesAsync(Guid userId);
    Task<bool> DeleteExpenseAsync(Guid userId, Guid expenseId);
    Task<ExpenseStatisticsDto> GetExpenseStatisticsAsync(string cityId);

    // 评论相关
    Task<UserCityReviewDto> UpsertReviewAsync(Guid userId, UpsertCityReviewRequest request);
    Task<List<UserCityReviewDto>> GetCityReviewsAsync(string cityId);
    Task<UserCityReviewDto?> GetUserReviewAsync(Guid userId, string cityId);
    Task<bool> DeleteReviewAsync(Guid userId, string cityId);

    // 统计相关
    Task<CityUserContentStatsDto> GetCityStatsAsync(string cityId);
}

public class UserCityContentService : IUserCityContentService
{
    private readonly string _connectionString;
    private readonly ILogger<UserCityContentService> _logger;

    public UserCityContentService(IConfiguration configuration, ILogger<UserCityContentService> logger)
    {
        _connectionString = configuration.GetConnectionString("SupabaseDb")
                            ?? throw new InvalidOperationException("SupabaseDb connection string not found");
        _logger = logger;
    }

    #region 统计相关

    public async Task<CityUserContentStatsDto> GetCityStatsAsync(string cityId)
    {
        await using var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync();

        var sql = @"
            SELECT 
                @CityId as city_id,
                COUNT(DISTINCT p.user_id) as photo_contributors,
                COUNT(DISTINCT e.user_id) as expense_contributors,
                COUNT(DISTINCT r.user_id) as review_contributors,
                COUNT(p.id) as photo_count,
                COUNT(e.id) as expense_count,
                COUNT(r.id) as review_count,
                COALESCE(AVG(r.rating), 0) as average_rating
            FROM (SELECT @CityId as city_id) c
            LEFT JOIN user_city_photos p ON c.city_id = p.city_id
            LEFT JOIN user_city_expenses e ON c.city_id = e.city_id
            LEFT JOIN user_city_reviews r ON c.city_id = r.city_id";

        await using var cmd = new NpgsqlCommand(sql, connection);
        cmd.Parameters.AddWithValue("CityId", cityId);

        await using var reader = await cmd.ExecuteReaderAsync();
        if (await reader.ReadAsync())
            return new CityUserContentStatsDto
            {
                CityId = cityId,
                PhotoContributors = reader.GetInt32(reader.GetOrdinal("photo_contributors")),
                ExpenseContributors = reader.GetInt32(reader.GetOrdinal("expense_contributors")),
                ReviewContributors = reader.GetInt32(reader.GetOrdinal("review_contributors")),
                PhotoCount = reader.GetInt32(reader.GetOrdinal("photo_count")),
                ExpenseCount = reader.GetInt32(reader.GetOrdinal("expense_count")),
                ReviewCount = reader.GetInt32(reader.GetOrdinal("review_count")),
                AverageRating = (decimal)reader.GetDouble(reader.GetOrdinal("average_rating"))
            };

        return new CityUserContentStatsDto { CityId = cityId };
    }

    #endregion

    #region 照片相关

    public async Task<UserCityPhotoDto> AddPhotoAsync(Guid userId, AddCityPhotoRequest request)
    {
        await using var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync();

        var sql = @"
            INSERT INTO user_city_photos (user_id, city_id, image_url, caption, location, taken_at)
            VALUES (@UserId, @CityId, @ImageUrl, @Caption, @Location, @TakenAt)
            RETURNING id, user_id, city_id, image_url, caption, location, taken_at, created_at";

        await using var cmd = new NpgsqlCommand(sql, connection);
        cmd.Parameters.AddWithValue("UserId", userId);
        cmd.Parameters.AddWithValue("CityId", request.CityId);
        cmd.Parameters.AddWithValue("ImageUrl", request.ImageUrl);
        cmd.Parameters.AddWithValue("Caption", (object?)request.Caption ?? DBNull.Value);
        cmd.Parameters.AddWithValue("Location", (object?)request.Location ?? DBNull.Value);
        cmd.Parameters.AddWithValue("TakenAt", (object?)request.TakenAt ?? DBNull.Value);

        await using var reader = await cmd.ExecuteReaderAsync();
        if (await reader.ReadAsync()) return MapPhotoFromReader(reader);

        throw new Exception("Failed to add photo");
    }

    public async Task<List<UserCityPhotoDto>> GetCityPhotosAsync(string cityId, Guid? userId = null)
    {
        await using var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync();

        var sql = userId.HasValue
            ? "SELECT * FROM user_city_photos WHERE city_id = @CityId AND user_id = @UserId ORDER BY created_at DESC"
            : "SELECT * FROM user_city_photos WHERE city_id = @CityId ORDER BY created_at DESC";

        await using var cmd = new NpgsqlCommand(sql, connection);
        cmd.Parameters.AddWithValue("CityId", cityId);
        if (userId.HasValue)
            cmd.Parameters.AddWithValue("UserId", userId.Value);

        var photos = new List<UserCityPhotoDto>();
        await using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync()) photos.Add(MapPhotoFromReader(reader));

        return photos;
    }

    public async Task<List<UserCityPhotoDto>> GetUserPhotosAsync(Guid userId)
    {
        await using var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync();

        var sql = "SELECT * FROM user_city_photos WHERE user_id = @UserId ORDER BY created_at DESC";

        await using var cmd = new NpgsqlCommand(sql, connection);
        cmd.Parameters.AddWithValue("UserId", userId);

        var photos = new List<UserCityPhotoDto>();
        await using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync()) photos.Add(MapPhotoFromReader(reader));

        return photos;
    }

    public async Task<bool> DeletePhotoAsync(Guid userId, Guid photoId)
    {
        await using var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync();

        var sql = "DELETE FROM user_city_photos WHERE id = @Id AND user_id = @UserId";

        await using var cmd = new NpgsqlCommand(sql, connection);
        cmd.Parameters.AddWithValue("Id", photoId);
        cmd.Parameters.AddWithValue("UserId", userId);

        return await cmd.ExecuteNonQueryAsync() > 0;
    }

    #endregion

    #region 费用相关

    public async Task<UserCityExpenseDto> AddExpenseAsync(Guid userId, AddCityExpenseRequest request)
    {
        await using var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync();

        var sql = @"
            INSERT INTO user_city_expenses (user_id, city_id, category, amount, currency, description, date)
            VALUES (@UserId, @CityId, @Category, @Amount, @Currency, @Description, @Date)
            RETURNING id, user_id, city_id, category, amount, currency, description, date, created_at";

        await using var cmd = new NpgsqlCommand(sql, connection);
        cmd.Parameters.AddWithValue("UserId", userId);
        cmd.Parameters.AddWithValue("CityId", request.CityId);
        cmd.Parameters.AddWithValue("Category", request.Category);
        cmd.Parameters.AddWithValue("Amount", request.Amount);
        cmd.Parameters.AddWithValue("Currency", request.Currency);
        cmd.Parameters.AddWithValue("Description", (object?)request.Description ?? DBNull.Value);
        cmd.Parameters.AddWithValue("Date", request.Date);

        await using var reader = await cmd.ExecuteReaderAsync();
        if (await reader.ReadAsync()) return MapExpenseFromReader(reader);

        throw new Exception("Failed to add expense");
    }

    public async Task<List<UserCityExpenseDto>> GetCityExpensesAsync(string cityId, Guid? userId = null)
    {
        await using var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync();

        var sql = userId.HasValue
            ? "SELECT * FROM user_city_expenses WHERE city_id = @CityId AND user_id = @UserId ORDER BY date DESC"
            : "SELECT * FROM user_city_expenses WHERE city_id = @CityId ORDER BY date DESC";

        await using var cmd = new NpgsqlCommand(sql, connection);
        cmd.Parameters.AddWithValue("CityId", cityId);
        if (userId.HasValue)
            cmd.Parameters.AddWithValue("UserId", userId.Value);

        var expenses = new List<UserCityExpenseDto>();
        await using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync()) expenses.Add(MapExpenseFromReader(reader));

        return expenses;
    }

    public async Task<List<UserCityExpenseDto>> GetUserExpensesAsync(Guid userId)
    {
        await using var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync();

        var sql = "SELECT * FROM user_city_expenses WHERE user_id = @UserId ORDER BY date DESC";

        await using var cmd = new NpgsqlCommand(sql, connection);
        cmd.Parameters.AddWithValue("UserId", userId);

        var expenses = new List<UserCityExpenseDto>();
        await using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync()) expenses.Add(MapExpenseFromReader(reader));

        return expenses;
    }

    public async Task<bool> DeleteExpenseAsync(Guid userId, Guid expenseId)
    {
        await using var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync();

        var sql = "DELETE FROM user_city_expenses WHERE id = @Id AND user_id = @UserId";

        await using var cmd = new NpgsqlCommand(sql, connection);
        cmd.Parameters.AddWithValue("Id", expenseId);
        cmd.Parameters.AddWithValue("UserId", userId);

        return await cmd.ExecuteNonQueryAsync() > 0;
    }

    public async Task<ExpenseStatisticsDto> GetExpenseStatisticsAsync(string cityId)
    {
        await using var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync();

        // 查询各分类的平均费用
        var categorySql = @"
            SELECT 
                category,
                AVG(amount) as average_cost
            FROM user_city_expenses 
            WHERE city_id = @CityId
            GROUP BY category";

        await using var categoryCmd = new NpgsqlCommand(categorySql, connection);
        categoryCmd.Parameters.AddWithValue("CityId", cityId);

        var categoryCosts = new Dictionary<string, decimal>();
        await using (var reader = await categoryCmd.ExecuteReaderAsync())
        {
            while (await reader.ReadAsync())
            {
                var category = reader.GetString(0);
                var avgCost = reader.GetDecimal(1);
                categoryCosts[category] = avgCost;
            }
        }

        // 查询统计信息
        var statsSql = @"
            SELECT 
                COUNT(DISTINCT user_id) as contributor_count,
                COUNT(*) as total_expense_count
            FROM user_city_expenses 
            WHERE city_id = @CityId";

        await using var statsCmd = new NpgsqlCommand(statsSql, connection);
        statsCmd.Parameters.AddWithValue("CityId", cityId);

        await using var statsReader = await statsCmd.ExecuteReaderAsync();
        if (await statsReader.ReadAsync())
        {
            return new ExpenseStatisticsDto
            {
                TotalAverageCost = categoryCosts.Values.Sum(),
                CategoryCosts = categoryCosts,
                ContributorCount = statsReader.GetInt32(0),
                TotalExpenseCount = statsReader.GetInt32(1),
                Currency = "USD",
                UpdatedAt = DateTime.UtcNow
            };
        }

        return new ExpenseStatisticsDto
        {
            TotalAverageCost = 0,
            CategoryCosts = new Dictionary<string, decimal>(),
            ContributorCount = 0,
            TotalExpenseCount = 0,
            Currency = "USD",
            UpdatedAt = DateTime.UtcNow
        };
    }

    #endregion

    #region 评论相关

    public async Task<UserCityReviewDto> UpsertReviewAsync(Guid userId, UpsertCityReviewRequest request)
    {
        await using var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync();

        var sql = @"
            INSERT INTO user_city_reviews (user_id, city_id, rating, title, content, visit_date)
            VALUES (@UserId, @CityId, @Rating, @Title, @Content, @VisitDate)
            ON CONFLICT (user_id, city_id) 
            DO UPDATE SET 
                rating = @Rating,
                title = @Title,
                content = @Content,
                visit_date = @VisitDate,
                updated_at = NOW()
            RETURNING id, user_id, city_id, rating, title, content, visit_date, created_at, updated_at";

        await using var cmd = new NpgsqlCommand(sql, connection);
        cmd.Parameters.AddWithValue("UserId", userId);
        cmd.Parameters.AddWithValue("CityId", request.CityId);
        cmd.Parameters.AddWithValue("Rating", request.Rating);
        cmd.Parameters.AddWithValue("Title", request.Title);
        cmd.Parameters.AddWithValue("Content", request.Content);
        cmd.Parameters.AddWithValue("VisitDate", (object?)request.VisitDate ?? DBNull.Value);

        await using var reader = await cmd.ExecuteReaderAsync();
        if (await reader.ReadAsync()) return MapReviewFromReader(reader);

        throw new Exception("Failed to upsert review");
    }

    public async Task<List<UserCityReviewDto>> GetCityReviewsAsync(string cityId)
    {
        await using var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync();

        var sql = "SELECT * FROM user_city_reviews WHERE city_id = @CityId ORDER BY created_at DESC";

        await using var cmd = new NpgsqlCommand(sql, connection);
        cmd.Parameters.AddWithValue("CityId", cityId);

        var reviews = new List<UserCityReviewDto>();
        await using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync()) reviews.Add(MapReviewFromReader(reader));

        return reviews;
    }

    public async Task<UserCityReviewDto?> GetUserReviewAsync(Guid userId, string cityId)
    {
        await using var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync();

        var sql = "SELECT * FROM user_city_reviews WHERE user_id = @UserId AND city_id = @CityId";

        await using var cmd = new NpgsqlCommand(sql, connection);
        cmd.Parameters.AddWithValue("UserId", userId);
        cmd.Parameters.AddWithValue("CityId", cityId);

        await using var reader = await cmd.ExecuteReaderAsync();
        if (await reader.ReadAsync()) return MapReviewFromReader(reader);

        return null;
    }

    public async Task<bool> DeleteReviewAsync(Guid userId, string cityId)
    {
        await using var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync();

        var sql = "DELETE FROM user_city_reviews WHERE user_id = @UserId AND city_id = @CityId";

        await using var cmd = new NpgsqlCommand(sql, connection);
        cmd.Parameters.AddWithValue("UserId", userId);
        cmd.Parameters.AddWithValue("CityId", cityId);

        return await cmd.ExecuteNonQueryAsync() > 0;
    }

    #endregion

    #region Helper Methods

    private UserCityPhotoDto MapPhotoFromReader(NpgsqlDataReader reader)
    {
        return new UserCityPhotoDto
        {
            Id = reader.GetGuid(reader.GetOrdinal("id")),
            UserId = reader.GetGuid(reader.GetOrdinal("user_id")),
            CityId = reader.GetString(reader.GetOrdinal("city_id")),
            ImageUrl = reader.GetString(reader.GetOrdinal("image_url")),
            Caption = reader.IsDBNull(reader.GetOrdinal("caption"))
                ? null
                : reader.GetString(reader.GetOrdinal("caption")),
            Description = TryGetString(reader, "description"),
            Location = reader.IsDBNull(reader.GetOrdinal("location"))
                ? null
                : reader.GetString(reader.GetOrdinal("location")),
            PlaceName = TryGetString(reader, "place_name"),
            Address = TryGetString(reader, "address"),
            Latitude = TryGetDouble(reader, "latitude"),
            Longitude = TryGetDouble(reader, "longitude"),
            TakenAt = reader.IsDBNull(reader.GetOrdinal("taken_at"))
                ? null
                : reader.GetDateTime(reader.GetOrdinal("taken_at")),
            CreatedAt = reader.GetDateTime(reader.GetOrdinal("created_at"))
        };
    }

    private static string? TryGetString(IDataRecord record, string columnName)
    {
        return HasColumn(record, columnName) && !record.IsDBNull(record.GetOrdinal(columnName))
            ? record.GetString(record.GetOrdinal(columnName))
            : null;
    }

    private static double? TryGetDouble(IDataRecord record, string columnName)
    {
        return HasColumn(record, columnName) && !record.IsDBNull(record.GetOrdinal(columnName))
            ? record.GetDouble(record.GetOrdinal(columnName))
            : null;
    }

    private static bool HasColumn(IDataRecord record, string columnName)
    {
        for (var i = 0; i < record.FieldCount; i++)
            if (record.GetName(i).Equals(columnName, StringComparison.OrdinalIgnoreCase))
                return true;

        return false;
    }

    private UserCityExpenseDto MapExpenseFromReader(NpgsqlDataReader reader)
    {
        return new UserCityExpenseDto
        {
            Id = reader.GetGuid(reader.GetOrdinal("id")),
            UserId = reader.GetGuid(reader.GetOrdinal("user_id")),
            CityId = reader.GetString(reader.GetOrdinal("city_id")),
            Category = reader.GetString(reader.GetOrdinal("category")),
            Amount = reader.GetDecimal(reader.GetOrdinal("amount")),
            Currency = reader.GetString(reader.GetOrdinal("currency")),
            Description = reader.IsDBNull(reader.GetOrdinal("description"))
                ? null
                : reader.GetString(reader.GetOrdinal("description")),
            Date = reader.GetDateTime(reader.GetOrdinal("date")),
            CreatedAt = reader.GetDateTime(reader.GetOrdinal("created_at"))
        };
    }

    private UserCityReviewDto MapReviewFromReader(NpgsqlDataReader reader)
    {
        return new UserCityReviewDto
        {
            Id = reader.GetGuid(reader.GetOrdinal("id")),
            UserId = reader.GetGuid(reader.GetOrdinal("user_id")),
            CityId = reader.GetString(reader.GetOrdinal("city_id")),
            Rating = reader.GetInt32(reader.GetOrdinal("rating")),
            Title = reader.GetString(reader.GetOrdinal("title")),
            Content = reader.GetString(reader.GetOrdinal("content")),
            VisitDate = reader.IsDBNull(reader.GetOrdinal("visit_date"))
                ? null
                : reader.GetDateTime(reader.GetOrdinal("visit_date")),
            CreatedAt = reader.GetDateTime(reader.GetOrdinal("created_at")),
            UpdatedAt = reader.GetDateTime(reader.GetOrdinal("updated_at"))
        };
    }

    #endregion
}