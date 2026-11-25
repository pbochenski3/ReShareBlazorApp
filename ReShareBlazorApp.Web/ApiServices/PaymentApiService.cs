using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Components.Authorization;
using ReShareBlazorApp.Shared.DTOs;
using ReShareBlazorApp.Shared.Enums;

namespace ReShareBlazorApp.Web.ApiServices;

public class PaymentApiService
{
    private readonly HttpClient _http;
    private readonly AuthenticationStateProvider _auth;

    public PaymentApiService(HttpClient http, AuthenticationStateProvider auth)
    {
        _http = http;
        _auth = auth;
    }

    public Task<List<OrderDTO>> GetMyOrdersAsync() => GetAsync("api/payments/orders");

    public Task<List<OrderDTO>> GetMySalesAsync() => GetAsync("api/payments/sales");

    public async Task<bool> UpdateStatusAsync(int paymentId, PaymentStatus status)
    {
        using var request = await CreateRequestAsync(
            HttpMethod.Patch,
            $"api/payments/{paymentId}/status");

        if (request is null)
            return false;

        request.Content = JsonContent.Create(status);
        using var response = await _http.SendAsync(request);
        return response.IsSuccessStatusCode;
    }

    public async Task<bool> UpdateBuyerStatusAsync(int paymentId, PaymentStatus status)
    {
        using var request = await CreateRequestAsync(
            HttpMethod.Patch,
            $"api/payments/{paymentId}/buyer-status");

        if (request is null)
            return false;

        request.Content = JsonContent.Create(status);
        using var response = await _http.SendAsync(request);
        return response.IsSuccessStatusCode;
    }

    public async Task<bool> CloseProductAsync(int paymentId)
    {
        using var request = await CreateRequestAsync(
            HttpMethod.Post,
            $"api/payments/{paymentId}/close-product");

        if (request is null)
            return false;

        using var response = await _http.SendAsync(request);
        return response.IsSuccessStatusCode;
    }

    private async Task<List<OrderDTO>> GetAsync(string url)
    {
        using var request = await CreateRequestAsync(HttpMethod.Get, url);
        if (request is null)
            return new List<OrderDTO>();

        using var response = await _http.SendAsync(request);
        if (!response.IsSuccessStatusCode)
            return new List<OrderDTO>();

        return await response.Content.ReadFromJsonAsync<List<OrderDTO>>()
            ?? new List<OrderDTO>();
    }

    private async Task<HttpRequestMessage?> CreateRequestAsync(HttpMethod method, string url)
    {
        var state = await _auth.GetAuthenticationStateAsync();
        var token = state.User.FindFirst("access_token")?.Value;
        if (string.IsNullOrWhiteSpace(token))
            return null;

        var request = new HttpRequestMessage(method, url);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return request;
    }
}
