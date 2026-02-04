using AccommodationService.Application.DTOs;
using AccommodationService.Application.Services;
using GoNomads.Shared.Services;
using Microsoft.AspNetCore.Mvc;

namespace AccommodationService.Controllers;

/// <summary>
///     酒店 API 控制器
///     Token 验证在 Gateway 完成，服务端使用 ICurrentUserService 获取用户信息
/// </summary>
[ApiController]
[Route("api/v1/hotels")]
public class HotelController : ControllerBase
{
    private readonly IHotelService _hotelService;
    private readonly ICurrentUserService _currentUser;
    private readonly ILogger<HotelController> _logger;

    public HotelController(
        IHotelService hotelService,
        ICurrentUserService currentUser,
        ILogger<HotelController> logger)
    {
        _hotelService = hotelService;
        _currentUser = currentUser;
        _logger = logger;
    }

    /// <summary>
    ///     获取酒店列表（分页）
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<HotelListResponse>> GetList([FromQuery] HotelQueryParameters parameters)
    {
        var result = await _hotelService.GetListAsync(parameters);
        return Ok(result);
    }

    /// <summary>
    ///     根据城市ID获取酒店列表
    /// </summary>
    [HttpGet("city/{cityId:guid}")]
    public async Task<ActionResult<List<HotelDto>>> GetByCityId(Guid cityId)
    {
        var hotels = await _hotelService.GetByCityIdAsync(cityId);
        return Ok(hotels);
    }

    /// <summary>
    ///     获取酒店详情
    /// </summary>
    [HttpGet("{id:guid}")]
    public async Task<ActionResult<HotelDto>> GetById(Guid id)
    {
        var hotel = await _hotelService.GetByIdAsync(id);
        if (hotel == null)
        {
            return NotFound(new { message = "Hotel not found" });
        }
        return Ok(hotel);
    }

    /// <summary>
    ///     创建酒店
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<HotelDto>> Create([FromBody] CreateHotelRequest request)
    {
        var userId = _currentUser.TryGetUserId();
        if (userId == null)
        {
            return Unauthorized(new { message = "需要登录", errors = new[] { "未检测到用户信息" } });
        }

        _logger.LogInformation("User {UserId} creating hotel: {HotelName}", userId, request.Name);

        try
        {
            var hotel = await _hotelService.CreateAsync(request, userId.Value);
            return CreatedAtAction(nameof(GetById), new { id = hotel.Id }, hotel);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating hotel");
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    ///     更新酒店（只有创建者可以更新）
    /// </summary>
    [HttpPut("{id:guid}")]
    public async Task<ActionResult<HotelDto>> Update(Guid id, [FromBody] UpdateHotelRequest request)
    {
        var userId = _currentUser.TryGetUserId();
        if (userId == null)
        {
            return Unauthorized(new { message = "需要登录", errors = new[] { "未检测到用户信息" } });
        }

        try
        {
            var isAdmin = _currentUser.IsAdmin();
            var hotel = await _hotelService.UpdateAsync(id, request, userId.Value, isAdmin);
            if (hotel == null)
            {
                return NotFound(new { message = "Hotel not found" });
            }
            return Ok(hotel);
        }
        catch (UnauthorizedAccessException)
        {
            return StatusCode(403, new { message = "您没有权限更新此酒店", errors = new[] { "只有酒店创建者或管理员可以更新" } });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating hotel {HotelId}", id);
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    ///     删除酒店（只有创建者或管理员可以删除）
    /// </summary>
    [HttpDelete("{id:guid}")]
    public async Task<ActionResult> Delete(Guid id)
    {
        var userId = _currentUser.TryGetUserId();
        if (userId == null)
        {
            return Unauthorized(new { message = "需要登录", errors = new[] { "未检测到用户信息" } });
        }

        try
        {
            var isAdmin = _currentUser.IsAdmin();
            var result = await _hotelService.DeleteAsync(id, userId.Value, isAdmin);
            if (!result)
            {
                return NotFound(new { message = "Hotel not found" });
            }
            return NoContent();
        }
        catch (UnauthorizedAccessException)
        {
            return StatusCode(403, new { message = "您没有权限删除此酒店", errors = new[] { "只有酒店创建者或管理员可以删除" } });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting hotel {HotelId}", id);
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    ///     获取我创建的酒店
    /// </summary>
    [HttpGet("my")]
    public async Task<ActionResult<List<HotelDto>>> GetMyHotels()
    {
        var userId = _currentUser.TryGetUserId();
        if (userId == null)
        {
            return Unauthorized(new { message = "需要登录", errors = new[] { "未检测到用户信息" } });
        }

        var hotels = await _hotelService.GetMyHotelsAsync(userId.Value);
        return Ok(hotels);
    }

    // ============================================================
    // 房型相关 API
    // ============================================================

    /// <summary>
    ///     获取酒店的房型列表
    /// </summary>
    [HttpGet("{hotelId:guid}/rooms")]
    public async Task<ActionResult<List<RoomTypeDto>>> GetRoomTypes(Guid hotelId)
    {
        var roomTypes = await _hotelService.GetRoomTypesAsync(hotelId);
        return Ok(roomTypes);
    }

    /// <summary>
    ///     获取房型详情
    /// </summary>
    [HttpGet("rooms/{roomTypeId:guid}")]
    public async Task<ActionResult<RoomTypeDto>> GetRoomType(Guid roomTypeId)
    {
        var roomType = await _hotelService.GetRoomTypeByIdAsync(roomTypeId);
        if (roomType == null)
        {
            return NotFound(new { message = "Room type not found" });
        }
        return Ok(roomType);
    }

    /// <summary>
    ///     为酒店添加房型
    /// </summary>
    [HttpPost("{hotelId:guid}/rooms")]
    public async Task<ActionResult<RoomTypeDto>> CreateRoomType(Guid hotelId, [FromBody] CreateRoomTypeRequest request)
    {
        var userId = _currentUser.TryGetUserId();
        if (userId == null)
        {
            return Unauthorized(new { message = "需要登录", errors = new[] { "未检测到用户信息" } });
        }

        try
        {
            var roomType = await _hotelService.CreateRoomTypeAsync(hotelId, request);
            return CreatedAtAction(nameof(GetRoomType), new { roomTypeId = roomType.Id }, roomType);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating room type for hotel {HotelId}", hotelId);
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    ///     更新房型
    /// </summary>
    [HttpPut("rooms/{roomTypeId:guid}")]
    public async Task<ActionResult<RoomTypeDto>> UpdateRoomType(Guid roomTypeId, [FromBody] UpdateRoomTypeRequest request)
    {
        var userId = _currentUser.TryGetUserId();
        if (userId == null)
        {
            return Unauthorized(new { message = "需要登录", errors = new[] { "未检测到用户信息" } });
        }

        try
        {
            var roomType = await _hotelService.UpdateRoomTypeAsync(roomTypeId, request);
            if (roomType == null)
            {
                return NotFound(new { message = "Room type not found" });
            }
            return Ok(roomType);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating room type {RoomTypeId}", roomTypeId);
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    ///     删除房型
    /// </summary>
    [HttpDelete("rooms/{roomTypeId:guid}")]
    public async Task<ActionResult> DeleteRoomType(Guid roomTypeId)
    {
        var userId = _currentUser.TryGetUserId();
        if (userId == null)
        {
            return Unauthorized(new { message = "需要登录", errors = new[] { "未检测到用户信息" } });
        }

        var result = await _hotelService.DeleteRoomTypeAsync(roomTypeId);
        if (!result)
        {
            return NotFound(new { message = "Room type not found" });
        }
        return NoContent();
    }
}
