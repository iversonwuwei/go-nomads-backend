using System;
using GoNomads.Shared.Exceptions;
using GoNomads.Shared.Models;

namespace GoNomads.Shared.Extensions;

/// <summary>
/// 为 <see cref="ApiResponse{T}"/> 提供统一的解包逻辑。
/// </summary>
public static class ApiResponseExtensions
{
    /// <summary>
    /// 解包 ApiResponse，如果 Success 为 false 则抛出 <see cref="ApiResponseException"/>。
    /// </summary>
    public static T? UnwrapOrThrow<T>(this ApiResponse<T> response, string? source = null)
    {
        ArgumentNullException.ThrowIfNull(response, nameof(response));

        if (response.Success)
        {
            return response.Data;
        }

        throw new ApiResponseException(
            response.Message,
            response.Errors,
            source);
    }

    /// <summary>
    /// 解包 ApiResponse，不抛出异常而是返回二元组 (Data, Success, Message, Errors)。
    /// </summary>
    public static ApiResponseUnwrapped<T> Unwrap<T>(this ApiResponse<T> response)
    {
        ArgumentNullException.ThrowIfNull(response, nameof(response));

        return new ApiResponseUnwrapped<T>(
            response.Data,
            response.Success,
            response.Message,
            response.Errors);
    }
}

public readonly record struct ApiResponseUnwrapped<T>(
    T? Data,
    bool Success,
    string Message,
    System.Collections.Generic.IReadOnlyList<string> Errors);
