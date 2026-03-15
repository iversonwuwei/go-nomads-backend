using System.Globalization;
using System.Net.Http.Json;
using System.Text.Json.Nodes;
using AccommodationService.Application.DTOs;

namespace AccommodationService.Services;

public class BookingDemandOptions
{
    public bool Enabled { get; set; }
    public bool UseSandbox { get; set; } = true;
    public string Token { get; set; } = string.Empty;
    public string AffiliateId { get; set; } = string.Empty;
    public string DefaultBookerCountry { get; set; } = "US";
    public string DefaultCurrency { get; set; } = "USD";
    public int SearchRadiusKm { get; set; } = 25;
    public int DefaultAdults { get; set; } = 2;
    public int DefaultRooms { get; set; } = 1;
    public int DefaultCheckInDaysAhead { get; set; } = 7;
    public int DefaultStayNights { get; set; } = 1;
    public int TimeoutSeconds { get; set; } = 20;
}

public class BookingHotelSearchRequest
{
    public string? CityName { get; set; }
    public string? CountryName { get; set; }
    public double? Latitude { get; set; }
    public double? Longitude { get; set; }
    public DateTime? CheckInDate { get; set; }
    public int? StayNights { get; set; }
    public int? AdultCount { get; set; }
    public int? RoomCount { get; set; }
    public string? Search { get; set; }
    public int Rows { get; set; } = 20;
}

public interface IBookingDemandClient
{
    bool IsConfigured { get; }
    Task<List<HotelDto>> SearchHotelsAsync(BookingHotelSearchRequest request, CancellationToken cancellationToken = default);
    Task<HotelDto?> GetHotelDetailsAsync(string hotelId, CancellationToken cancellationToken = default);
}

public class BookingDemandClient : IBookingDemandClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<BookingDemandClient> _logger;
    private readonly BookingDemandOptions _options;

    public BookingDemandClient(
        HttpClient httpClient,
        Microsoft.Extensions.Options.IOptions<BookingDemandOptions> options,
        ILogger<BookingDemandClient> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
        _options = options.Value;
    }

    public bool IsConfigured => _options.Enabled &&
                                !string.IsNullOrWhiteSpace(_options.Token) &&
                                !string.IsNullOrWhiteSpace(_options.AffiliateId);

    public async Task<List<HotelDto>> SearchHotelsAsync(BookingHotelSearchRequest request, CancellationToken cancellationToken = default)
    {
        if (!IsConfigured)
        {
            return new List<HotelDto>();
        }

        try
        {
            var checkIn = (request.CheckInDate ?? DateTime.UtcNow.Date.AddDays(_options.DefaultCheckInDaysAhead)).Date;
            var stayNights = request.StayNights.HasValue && request.StayNights.Value > 0
                ? request.StayNights.Value
                : Math.Max(_options.DefaultStayNights, 1);
            var adultCount = request.AdultCount.HasValue && request.AdultCount.Value > 0
                ? request.AdultCount.Value
                : Math.Max(_options.DefaultAdults, 1);
            var roomCount = request.RoomCount.HasValue && request.RoomCount.Value > 0
                ? request.RoomCount.Value
                : Math.Max(_options.DefaultRooms, 1);
            var checkOut = checkIn.AddDays(stayNights);
            var body = new JsonObject
            {
                ["booker"] = new JsonObject
                {
                    ["country"] = ResolveBookerCountry(request.CountryName),
                    ["platform"] = "android"
                },
                ["checkin"] = checkIn.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
                ["checkout"] = checkOut.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
                ["guests"] = new JsonObject
                {
                    ["number_of_adults"] = adultCount,
                    ["number_of_rooms"] = roomCount
                },
                ["extras"] = new JsonArray("products", "extra_charges"),
                ["rows"] = request.Rows,
                ["currency"] = _options.DefaultCurrency
            };

            if (!string.IsNullOrWhiteSpace(request.CityName))
            {
                body["city"] = request.CityName;
            }

            if (request.Latitude.HasValue && request.Longitude.HasValue)
            {
                body["coordinates"] = new JsonObject
                {
                    ["latitude"] = request.Latitude.Value,
                    ["longitude"] = request.Longitude.Value,
                    ["radius"] = _options.SearchRadiusKm
                };
            }

            var response = await _httpClient.PostAsJsonAsync("accommodations/search", body, cancellationToken);
            response.EnsureSuccessStatusCode();

            var payload = await response.Content.ReadFromJsonAsync<JsonNode>(cancellationToken: cancellationToken);
            var hotels = payload?["data"]?.AsArray()
                .Select(node => MapHotel(node, request.CityName, request.CountryName))
                .Where(dto => dto != null)
                .Cast<HotelDto>()
                .ToList() ?? new List<HotelDto>();

            if (string.IsNullOrWhiteSpace(request.Search))
            {
                return hotels;
            }

            var keyword = request.Search.Trim().ToLowerInvariant();
            return hotels.Where(h =>
                    h.Name.Contains(keyword, StringComparison.OrdinalIgnoreCase) ||
                    (h.Description?.Contains(keyword, StringComparison.OrdinalIgnoreCase) ?? false) ||
                    h.Address.Contains(keyword, StringComparison.OrdinalIgnoreCase))
                .ToList();
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            _logger.LogWarning("Booking Demand hotel search timed out. Falling back to internal hotels only.");
            return new List<HotelDto>();
        }
        catch (HttpRequestException ex)
        {
            _logger.LogWarning(ex, "Booking Demand hotel search failed. Falling back to internal hotels only.");
            return new List<HotelDto>();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Unexpected Booking Demand hotel search error. Falling back to internal hotels only.");
            return new List<HotelDto>();
        }
    }

    public async Task<HotelDto?> GetHotelDetailsAsync(string hotelId, CancellationToken cancellationToken = default)
    {
        if (!IsConfigured)
        {
            return null;
        }

        var bookingId = NormalizeBookingHotelId(hotelId);
        if (bookingId == null)
        {
            return null;
        }

        try
        {
            var response = await _httpClient.PostAsJsonAsync("accommodations/details", new JsonObject
            {
                ["accommodations"] = new JsonArray(bookingId.Value),
                ["extras"] = new JsonArray("description", "facilities", "photos", "payment", "rooms")
            }, cancellationToken);
            response.EnsureSuccessStatusCode();

            var payload = await response.Content.ReadFromJsonAsync<JsonNode>(cancellationToken: cancellationToken);
            var first = payload?["data"]?.AsArray().FirstOrDefault();
            return first == null ? null : MapHotel(first, null, null);
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            _logger.LogWarning("Booking Demand hotel details timed out for {HotelId}. Returning no external detail.", hotelId);
            return null;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogWarning(ex, "Booking Demand hotel details failed for {HotelId}. Returning no external detail.", hotelId);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Unexpected Booking Demand hotel details error for {HotelId}. Returning no external detail.", hotelId);
            return null;
        }
    }

    private HotelDto? MapHotel(JsonNode? node, string? cityName, string? countryName)
    {
        var id = ReadString(node, new[]
        {
            "id",
            "accommodation_id",
            "basic_property_data.id"
        });
        var name = ReadString(node, new[]
        {
            "name",
            "accommodation_name",
            "basic_property_data.name"
        });

        if (string.IsNullOrWhiteSpace(id) || string.IsNullOrWhiteSpace(name))
        {
            return null;
        }

        var mappedCityName = cityName ?? ReadString(node, new[]
        {
            "city_name",
            "location.city",
            "basic_property_data.city"
        });

        var mappedCountryName = countryName ?? ReadString(node, new[]
        {
            "country_name",
            "location.country",
            "basic_property_data.country_name"
        });

        return new HotelDto
        {
            Id = $"booking_{id}",
            Source = "booking",
            ExternalStatus = "live",
            Name = name,
            Description = ReadString(node, new[]
            {
                "description",
                "basic_property_data.description",
                "property.wishlist_name"
            }),
            Address = BuildAddress(node),
            CityId = null,
            CityName = mappedCityName,
            Country = mappedCountryName,
            Latitude = ReadDecimal(node, new[]
            {
                "coordinates.latitude",
                "location.latitude",
                "basic_property_data.location.latitude",
                "latitude"
            }),
            Longitude = ReadDecimal(node, new[]
            {
                "coordinates.longitude",
                "location.longitude",
                "basic_property_data.location.longitude",
                "longitude"
            }),
            Rating = ReadDecimal(node, new[]
            {
                "review_score",
                "reviewScore",
                "basic_property_data.review_score"
            }) ?? 0,
            ReviewCount = ReadInt(node, new[]
            {
                "review_nr",
                "review_count",
                "reviewCount",
                "basic_property_data.review_nr"
            }) ?? 0,
            Images = ExtractImages(node).ToArray(),
            Category = ReadString(node, new[]
            {
                "accommodation_type_name",
                "type",
                "property_class",
                "basic_property_data.accommodation_type_name"
            }) ?? "hotel",
            StarRating = ReadInt(node, new[]
            {
                "stars",
                "star_rating",
                "basic_property_data.stars"
            }),
            PricePerNight = ReadDecimal(node, new[]
            {
                "price.book",
                "price.base",
                "price.total",
                "price.amount",
                "price_breakdown.gross_amount",
                "composite_price_breakdown.gross_amount_per_night.value",
                "composite_price_breakdown.gross_amount.value",
                "basic_property_data.price",
                "products.0.price.base",
                "products.0.price.book",
                "products.0.price.total"
            }) ?? 0,
            Currency = ReadString(node, new[]
            {
                "currency",
                "price.currency",
                "price_breakdown.currency",
                "composite_price_breakdown.gross_amount.currency",
                "basic_property_data.currency",
                "products.0.price.currency"
            }) ?? _options.DefaultCurrency,
            IsFeatured = (ReadDecimal(node, new[] { "review_score", "basic_property_data.review_score" }) ?? 0) >= 8.5m,
            Phone = ReadString(node, new[] { "contact.phone", "phone" }),
            Website = ReadString(node, new[] { "url", "deeplink", "webpage" }),
            HasWifi = ContainsAmenity(node, "wifi", "wi-fi"),
            HasWorkDesk = ContainsAmenity(node, "desk", "work desk", "workspace"),
            HasCoworkingSpace = ContainsAmenity(node, "coworking", "business centre", "business center"),
            HasAirConditioning = ContainsAmenity(node, "air conditioning"),
            HasKitchen = ContainsAmenity(node, "kitchen", "kitchenette"),
            HasLaundry = ContainsAmenity(node, "laundry"),
            HasParking = ContainsAmenity(node, "parking"),
            HasPool = ContainsAmenity(node, "pool"),
            HasGym = ContainsAmenity(node, "gym", "fitness"),
            Has24HReception = ContainsAmenity(node, "24-hour front desk", "24 hour front desk", "24-hour reception"),
            IsPetFriendly = ContainsAmenity(node, "pet", "pets allowed"),
            NomadScore = BuildNomadScore(node),
            CreatedAt = DateTime.UtcNow,
            RoomTypes = new List<RoomTypeDto>()
        };
    }

    private string ResolveBookerCountry(string? countryName)
    {
        if (string.IsNullOrWhiteSpace(countryName))
        {
            return _options.DefaultBookerCountry;
        }

        return countryName.Length == 2 ? countryName.ToUpperInvariant() : _options.DefaultBookerCountry;
    }

    private static int? NormalizeBookingHotelId(string hotelId)
    {
        var normalized = hotelId.Replace("booking_", string.Empty, StringComparison.OrdinalIgnoreCase);
        return int.TryParse(normalized, out var numericId) ? numericId : null;
    }

    private static string BuildAddress(JsonNode? node)
    {
        var candidates = new[]
        {
            ReadString(node, new[] { "address" }),
            ReadString(node, new[] { "address_trans" }),
            ReadString(node, new[] { "location.address" }),
            ReadString(node, new[] { "basic_property_data.address" })
        }.Where(value => !string.IsNullOrWhiteSpace(value)).ToList();

        return candidates.FirstOrDefault() ?? string.Empty;
    }

    private static List<string> ExtractImages(JsonNode? node)
    {
        var results = new List<string>();
        foreach (var path in new[] { "photos", "images", "basic_property_data.photos" })
        {
            if (ReadNode(node, path) is JsonArray array)
            {
                foreach (var item in array)
                {
                    var url = item switch
                    {
                        JsonValue value => value.ToString(),
                        JsonObject => ReadString(item, new[] { "url_max300", "url_original", "url", "photo_url" }),
                        _ => null
                    };

                    if (!string.IsNullOrWhiteSpace(url))
                    {
                        results.Add(url!);
                    }
                }
            }
        }

        if (results.Count == 0)
        {
            var fallback = ReadString(node, new[] { "main_photo_url", "photo_main_url", "basic_property_data.main_photo_url" });
            if (!string.IsNullOrWhiteSpace(fallback))
            {
                results.Add(fallback);
            }
        }

        return results;
    }

    private static bool ContainsAmenity(JsonNode? node, params string[] keywords)
    {
        var values = new List<string>();
        foreach (var path in new[] { "facilities", "facility_types", "amenities", "basic_property_data.facilities" })
        {
            if (ReadNode(node, path) is JsonArray array)
            {
                values.AddRange(array.Select(item => item?.ToJsonString() ?? string.Empty));
            }
        }

        return values.Any(value => keywords.Any(keyword => value.Contains(keyword, StringComparison.OrdinalIgnoreCase)));
    }

    private static int BuildNomadScore(JsonNode? node)
    {
        var score = 0;
        if (ContainsAmenity(node, "wifi", "wi-fi")) score += 25;
        if (ContainsAmenity(node, "desk", "workspace")) score += 20;
        if (ContainsAmenity(node, "coworking", "business centre", "business center")) score += 20;
        if (ContainsAmenity(node, "air conditioning")) score += 10;
        if (ContainsAmenity(node, "kitchen", "laundry")) score += 10;
        if ((ReadDecimal(node, new[] { "review_score", "basic_property_data.review_score" }) ?? 0) >= 8m) score += 15;
        return Math.Min(score, 100);
    }

    private static string? ReadString(JsonNode? node, IEnumerable<string> paths)
    {
        foreach (var path in paths)
        {
            if (ReadNode(node, path) is JsonValue value)
            {
                var str = value.ToString();
                if (!string.IsNullOrWhiteSpace(str))
                {
                    return str;
                }
            }
        }

        return null;
    }

    private static int? ReadInt(JsonNode? node, IEnumerable<string> paths)
    {
        foreach (var path in paths)
        {
            if (ReadNode(node, path) is JsonValue value)
            {
                if (value.TryGetValue<int>(out var intValue)) return intValue;
                if (int.TryParse(value.ToString(), out intValue)) return intValue;
            }
        }

        return null;
    }

    private static decimal? ReadDecimal(JsonNode? node, IEnumerable<string> paths)
    {
        foreach (var path in paths)
        {
            if (ReadNode(node, path) is JsonValue value)
            {
                if (value.TryGetValue<decimal>(out var decimalValue)) return decimalValue;
                if (decimal.TryParse(value.ToString(), NumberStyles.Any, CultureInfo.InvariantCulture, out decimalValue))
                {
                    return decimalValue;
                }
            }
        }

        return null;
    }

    private static JsonNode? ReadNode(JsonNode? node, string path)
    {
        var current = node;
        foreach (var segment in path.Split('.'))
        {
            if (current == null)
            {
                return null;
            }

            if (int.TryParse(segment, out var index))
            {
                if (current is not JsonArray array || index >= array.Count)
                {
                    return null;
                }

                current = array[index];
                continue;
            }

            current = current[segment];
        }

        return current;
    }
}