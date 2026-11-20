using Microsoft.AspNetCore.Components.Authorization;
using ReShareBlazorApp.Shared.DTOs;
using System.Net.Http.Headers;
using System.Net.Http.Json;

namespace ReShareBlazorApp.Web.ApiServices
{
    public class AdminApiService
    {
        private readonly HttpClient _httpClient;
        private readonly AuthenticationStateProvider _authStateProvider;
        public AdminApiService(HttpClient httpClient, AuthenticationStateProvider authStateProvider)
        {
            _httpClient = httpClient;
            _authStateProvider = authStateProvider;
        }

        // Buduje request z tokenem JWT zalogowanego admina (tak jak w ProductApiService)
        private async Task<HttpRequestMessage> CreateRequestAsync(HttpMethod method, string url)
        {
            var authState = await _authStateProvider.GetAuthenticationStateAsync();
            var token = authState.User.FindFirst("access_token")?.Value;

            var request = new HttpRequestMessage(method, url);
            if (!string.IsNullOrEmpty(token))
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            return request;
        }

        public async Task<AdminStatsDTO?> GetStatsAsync()
        {
            using var request = await CreateRequestAsync(HttpMethod.Get, "api/admin/stats");
            var response = await _httpClient.SendAsync(request);
            if (!response.IsSuccessStatusCode) return null;
            return await response.Content.ReadFromJsonAsync<AdminStatsDTO>();
        }

        public async Task<List<AdminUserDTO>> GetUsersAsync()
        {
            using var request = await CreateRequestAsync(HttpMethod.Get, "api/admin/users");
            var response = await _httpClient.SendAsync(request);
            if (!response.IsSuccessStatusCode) return new();
            return await response.Content.ReadFromJsonAsync<List<AdminUserDTO>>() ?? new();
        }

        public async Task<bool> SetUserActiveAsync(int userId, bool isActive)
        {
            using var request = await CreateRequestAsync(
                HttpMethod.Post,
                $"api/admin/users/{userId}/active?value={isActive.ToString().ToLower()}");
            var response = await _httpClient.SendAsync(request);
            return response.IsSuccessStatusCode;
        }

        public async Task<List<ProductDTO>> GetProductsAsync()
        {
            using var request = await CreateRequestAsync(HttpMethod.Get, "api/admin/products");
            var response = await _httpClient.SendAsync(request);
            if (!response.IsSuccessStatusCode) return new();
            return await response.Content.ReadFromJsonAsync<List<ProductDTO>>() ?? new();
        }

        public async Task<bool> DeleteProductAsync(int productId)
        {
            using var request = await CreateRequestAsync(HttpMethod.Delete, $"api/admin/products/{productId}");
            var response = await _httpClient.SendAsync(request);
            return response.IsSuccessStatusCode;
        }

        public async Task<LockdownDTO?> GetLockdownAsync()
        {
            try
            {
                using var request = await CreateRequestAsync(HttpMethod.Get, "api/lockdown");
                var response = await _httpClient.SendAsync(request);
                if (!response.IsSuccessStatusCode) return null;
                return await response.Content.ReadFromJsonAsync<LockdownDTO>();
            }
            catch
            {
                return null;
            }
        }

        public async Task<bool> SetLockdownAsync(LockdownDTO dto)
        {
            try
            {
                using var request = await CreateRequestAsync(HttpMethod.Put, "api/admin/lockdown");
                request.Content = JsonContent.Create(dto);
                var response = await _httpClient.SendAsync(request);
                return response.IsSuccessStatusCode;
            }
            catch
            {
                return false;
            }
        }
    }
}
