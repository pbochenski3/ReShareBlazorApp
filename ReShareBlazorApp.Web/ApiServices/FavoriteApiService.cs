using Microsoft.AspNetCore.Components.Authorization;
using ReShareBlazorApp.Shared.DTOs;
using System.Net.Http.Headers;

namespace ReShareBlazorApp.Web.ApiServices
{
    public class FavoriteApiService
    {
        private readonly HttpClient _httpClient;
        private readonly AuthenticationStateProvider _authStateProvider;
        public FavoriteApiService(HttpClient httpClient, AuthenticationStateProvider authStateProvider)
        {
            _httpClient = httpClient;
            _authStateProvider = authStateProvider;
        }

        // token JWT zalogowanego użytkownika (zapisany w cookie jako claim "access_token")
        private async Task<string?> GetTokenAsync()
        {
            var authState = await _authStateProvider.GetAuthenticationStateAsync();
            return authState.User.FindFirst("access_token")?.Value;
        }

        private static HttpRequestMessage CreateRequest(HttpMethod method, string url, string token)
        {
            var request = new HttpRequestMessage(method, url);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            return request;
        }

        // Numery ulubionych ogłoszeń (do pokolorowania serduszek). Gdy niezalogowany -> pusta lista.
        public async Task<HashSet<int>> GetFavoriteIdsAsync()
        {
            try
            {
                var token = await GetTokenAsync();
                if (string.IsNullOrEmpty(token))
                    return new HashSet<int>();
                using var request = CreateRequest(HttpMethod.Get, "api/favorite/ids", token);
                var response = await _httpClient.SendAsync(request);
                if (!response.IsSuccessStatusCode)
                    return new HashSet<int>();
                var ids = await response.Content.ReadFromJsonAsync<List<int>>() ?? new List<int>();
                return ids.ToHashSet();
            }
            catch (Exception)
            {
                return new HashSet<int>(); // strona ma się wyświetlić nawet gdy API chwilowo nie działa
            }
        }

        // Pełne ogłoszenia z ulubionych (zakładka "Ulubione" w profilu)
        public async Task<List<ProductDTO>> GetFavoritesAsync()
        {
            try
            {
                var token = await GetTokenAsync();
                if (string.IsNullOrEmpty(token))
                    return new List<ProductDTO>();
                using var request = CreateRequest(HttpMethod.Get, "api/favorite", token);
                var response = await _httpClient.SendAsync(request);
                if (!response.IsSuccessStatusCode)
                    return new List<ProductDTO>();
                return await response.Content.ReadFromJsonAsync<List<ProductDTO>>() ?? new List<ProductDTO>();
            }
            catch (Exception)
            {
                return new List<ProductDTO>();
            }
        }

        // Dodaje/usuwa ogłoszenie z ulubionych. Token podajemy ręcznie, bo wywołuje to endpoint
        // /favorites/toggle (poza komponentami Blazora). Zwraca wynik albo opis błędu.
        public async Task<FavoriteToggleResult> ToggleAsync(int productId, string token)
        {
            try
            {
                using var request = CreateRequest(HttpMethod.Post, $"api/favorite/{productId}/toggle", token);
                var response = await _httpClient.SendAsync(request);
                if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                    return new FavoriteToggleResult(null, "Sesja wygasła – zaloguj się ponownie.", true);
                if (!response.IsSuccessStatusCode)
                {
                    var body = await response.Content.ReadAsStringAsync();
                    return new FavoriteToggleResult(null, $"API zwróciło błąd {(int)response.StatusCode}: {Shorten(body)}", false);
                }
                var result = await response.Content.ReadFromJsonAsync<FavoriteToggleResponse>();
                return result is null
                    ? new FavoriteToggleResult(null, "Pusta odpowiedź z API.", false)
                    : new FavoriteToggleResult(result.IsFavorite, null, false);
            }
            catch (Exception ex)
            {
                return new FavoriteToggleResult(null, $"Nie można połączyć się z API: {ex.Message}", false);
            }
        }

        private static string Shorten(string text) => text.Length <= 300 ? text : text[..300] + "…";
    }

    // Wynik przełączenia ulubionych: IsFavorite albo Error (Unauthorized = trzeba się zalogować)
    public record FavoriteToggleResult(bool? IsFavorite, string? Error, bool Unauthorized);
}
