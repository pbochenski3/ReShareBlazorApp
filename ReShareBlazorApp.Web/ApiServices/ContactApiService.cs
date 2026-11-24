using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Components.Authorization;
using ReShareBlazorApp.Shared.DTOs;

namespace ReShareBlazorApp.Web.ApiServices;

public class ContactApiService
{
    private readonly HttpClient _httpClient;
    private readonly AuthenticationStateProvider _authStateProvider;

    public ContactApiService(HttpClient httpClient, AuthenticationStateProvider authStateProvider)
    {
        _httpClient = httpClient;
        _authStateProvider = authStateProvider;
    }

    private async Task<HttpRequestMessage> CreateRequestAsync(HttpMethod method, string url)
    {
        var authState = await _authStateProvider.GetAuthenticationStateAsync();
        var token = authState.User.FindFirst("access_token")?.Value;

        var request = new HttpRequestMessage(method, url);
        if (!string.IsNullOrEmpty(token))
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return request;
    }

    // Publiczny endpoint (nie wymaga tokena)
    public async Task<bool> SendContactMessageAsync(ContactRequest request)
    {
        var response = await _httpClient.PostAsJsonAsync("api/contact", request);
        return response.IsSuccessStatusCode;
    }

    // Endpointy administratora (z tokenem JWT)
    public async Task<List<ContactMessageDTO>?> GetContactMessagesAsync()
    {
        using var request = await CreateRequestAsync(HttpMethod.Get, "api/contact");
        var response = await _httpClient.SendAsync(request);
        if (!response.IsSuccessStatusCode) return null;
        return await response.Content.ReadFromJsonAsync<List<ContactMessageDTO>>();
    }

    public async Task<bool> MarkContactMessageAsReadAsync(int id)
    {
        using var request = await CreateRequestAsync(HttpMethod.Patch, $"api/contact/{id}/read");
        var response = await _httpClient.SendAsync(request);
        return response.IsSuccessStatusCode;
    }

    public async Task<bool> DeleteContactMessageAsync(int id)
    {
        using var request = await CreateRequestAsync(HttpMethod.Delete, $"api/contact/{id}");
        var response = await _httpClient.SendAsync(request);
        return response.IsSuccessStatusCode;
    }
}