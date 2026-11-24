
using Microsoft.AspNetCore.Components.Authorization;
using ReShareBlazorApp.Shared.DTOs;
using System.Net.Http.Headers;
using System.Net.Http.Json;

namespace ReShareBlazorApp.Web.ApiServices
{
    public class BadgeApiService
    {
        private readonly HttpClient _httpClient;
        private readonly AuthenticationStateProvider _authStateProvider;

        public BadgeApiService(
            HttpClient httpClient,
            AuthenticationStateProvider authStateProvider)
        {
            _httpClient = httpClient;
            _authStateProvider = authStateProvider;
        }

        // Pobieranie tokenu użytkownika
        private async Task<string> GetTokenAsync()
        {
            var authState = await _authStateProvider.GetAuthenticationStateAsync();

            var token = authState.User.FindFirst("access_token")?.Value;

            if (string.IsNullOrEmpty(token))
                throw new Exception("Brak tokenu uwierzytelniającego!");

            return token;
        }

        // Pobieranie odznak zalogowanego użytkownika
        public async Task<List<BadgeDTO>> GetMyBadgesAsync()
        {
            var token = await GetTokenAsync();

            using var request = new HttpRequestMessage(
                HttpMethod.Get,
                "api/badge/my"
            );

            request.Headers.Authorization =
                new AuthenticationHeaderValue("Bearer", token);

            var response = await _httpClient.SendAsync(request);

            response.EnsureSuccessStatusCode();

            return await response.Content.ReadFromJsonAsync<List<BadgeDTO>>()
                   ?? new List<BadgeDTO>();
        }

        // Pobieranie wszystkich odznak - administrator
        public async Task<List<BadgeDTO>> GetAllBadgesAsync()
        {
            var token = await GetTokenAsync();

            using var request = new HttpRequestMessage(
                HttpMethod.Get,
                "api/badge/all"
            );

            request.Headers.Authorization =
                new AuthenticationHeaderValue("Bearer", token);

            var response = await _httpClient.SendAsync(request);

            response.EnsureSuccessStatusCode();

            return await response.Content.ReadFromJsonAsync<List<BadgeDTO>>()
                   ?? new List<BadgeDTO>();
        }

        // Dodawanie nowej odznaki - administrator

        // Ręczne przyznawanie odznaki użytkownikowi - administrator
        public async Task<bool> AwardBadgeToUserAsync(int userId, int badgeId)
        {
            var token = await GetTokenAsync();

            using var request = new HttpRequestMessage(
                HttpMethod.Post,
                $"api/badge/user/{userId}/award/{badgeId}"
            );

            request.Headers.Authorization =
                new AuthenticationHeaderValue("Bearer", token);

            var response = await _httpClient.SendAsync(request);

            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync();
                throw new Exception($"Nie udało się przyznać odznaki: {error}");
            }

            return true;
        }

        // Odbieranie odznaki użytkownikowi - administrator
        public async Task<bool> RemoveBadgeFromUserAsync(int userId, int badgeId)
        {
            var token = await GetTokenAsync();

            using var request = new HttpRequestMessage(
                HttpMethod.Delete,
                $"api/badge/user/{userId}/remove/{badgeId}"
            );

            request.Headers.Authorization =
                new AuthenticationHeaderValue("Bearer", token);

            var response = await _httpClient.SendAsync(request);

            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync();
                throw new Exception($"Nie udało się odebrać odznaki: {error}");
            }

            return true;
        }

        public async Task<List<BadgeDTO>> GetUserBadgesAsync(int userId)
        {
            var response = await _httpClient.GetAsync($"api/badge/user/{userId}");

            response.EnsureSuccessStatusCode();

            return await response.Content.ReadFromJsonAsync<List<BadgeDTO>>()
                   ?? new List<BadgeDTO>();
        }

    }
}
