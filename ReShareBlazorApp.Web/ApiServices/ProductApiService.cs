using Microsoft.AspNetCore.Components.Authorization;
using ReShareBlazorApp.Api.Date.Enums;
using ReShareBlazorApp.Shared.DTOs;
using ReShareBlazorApp.Web.Models;
using System.Globalization;
using System.Net.Http.Headers;

namespace ReShareBlazorApp.Web.ApiServices
{
    public class ProductApiService
    {
        private readonly HttpClient _httpClient;
        private readonly AuthenticationStateProvider _authStateProvider;
        public ProductApiService(HttpClient httpClient, AuthenticationStateProvider authStateProvider)
        {
            _httpClient = httpClient;
            _authStateProvider = authStateProvider;
        }
        public async Task<(bool Success, string Message)> AddProductAsync(ProductModel productModel)
        {
            var authState = await _authStateProvider.GetAuthenticationStateAsync();
            var token = authState.User.FindFirst("access_token")?.Value;
            if (string.IsNullOrEmpty(token))
                return (false, "Brak tokenu uwierzytelniającego!");

            var product = new ProductDTO
            {
                Name = productModel.Name,
                Description = productModel.Description,
                Price = productModel.Price,
                ExpiresAt = productModel.ExpiresAt,
                Category = productModel.Category,
                City = productModel.City,
                Type = productModel.Type,
                Images = productModel.Images
                    .Select(i => new ProductImageDTO
                    {
                        FileName = i.FileName,
                        ContentType = i.ContentType,
                        Data = i.Data
                    })
                    .ToList()
            };
            using var request = new HttpRequestMessage(
                HttpMethod.Post,
                "api/product/addProduct"
                );
            request.Headers.Authorization =
                new AuthenticationHeaderValue("Bearer", token);
            request.Content = JsonContent.Create(product);
            var response = await _httpClient.SendAsync(request);
            if (!response.IsSuccessStatusCode)
            {
                // API zwraca komunikat walidacji zdjęć w treści odpowiedzi 400
                var error = response.StatusCode == System.Net.HttpStatusCode.BadRequest
                    ? (await response.Content.ReadAsStringAsync()).Trim('"')
                    : "";
                return (false, string.IsNullOrWhiteSpace(error)
                    ? "Nie udało się dodać produktu!"
                    : error);
            }
            return (true, "Produkt dodany pomyślnie!");
        }

        public async Task<string> UpdateProductAsync(int productId, ProductModel productModel)
        {
            var authState = await _authStateProvider.GetAuthenticationStateAsync();
            var token = authState.User.FindFirst("access_token")?.Value;
            if (string.IsNullOrEmpty(token))
                return "Brak tokenu uwierzytelniającego!";

            var product = new ProductUpdateDTO
            {
                Name = productModel.Name,
                Description = productModel.Description,
                Price = productModel.Price,
                ExpiresAt = productModel.ExpiresAt,
                Category = productModel.Category,
                City = productModel.City,
                Type = productModel.Type
            };

            using var request = new HttpRequestMessage(
                HttpMethod.Put,
                $"api/product/{productId}");
            request.Headers.Authorization =
                new AuthenticationHeaderValue("Bearer", token);
            request.Content = JsonContent.Create(product);

            var response = await _httpClient.SendAsync(request);
            if (!response.IsSuccessStatusCode)
                return "Nie udało się edytować ogłoszenia!";

            return "Ogłoszenie zostało zaktualizowane!";
        }

        public async Task<string> DeleteProductAsync(int productId)
        {
            var authState = await _authStateProvider.GetAuthenticationStateAsync();
            var token = authState.User.FindFirst("access_token")?.Value;
            if (string.IsNullOrEmpty(token))
                return "Brak tokenu uwierzytelniającego!";

            using var request = new HttpRequestMessage(
                HttpMethod.Delete,
                $"api/product/{productId}");
            request.Headers.Authorization =
                new AuthenticationHeaderValue("Bearer", token);

            var response = await _httpClient.SendAsync(request);
            if (!response.IsSuccessStatusCode)
                return "Nie udało się usunąć ogłoszenia!";

            return "Ogłoszenie zostało usunięte!";
        }
        public async Task<string> MarkAsSoldAsync(int productId)
        {
            var authState = await _authStateProvider.GetAuthenticationStateAsync();
            var token = authState.User.FindFirst("access_token")?.Value;
            if (string.IsNullOrEmpty(token))
                return "Brak tokenu uwierzytelniającego!";

            using var request = new HttpRequestMessage(
                HttpMethod.Post,
                $"api/product/{productId}/sold");
            request.Headers.Authorization =
                new AuthenticationHeaderValue("Bearer", token);

            var response = await _httpClient.SendAsync(request);
            if (!response.IsSuccessStatusCode)
                return "Nie udało się oznaczyć ogłoszenia jako sprzedane!";

            return "Ogłoszenie zostało oznaczone jako sprzedane!";
        }
        public async Task<List<ProductDTO>> GetProductsAsync(
       int number = 5,
       ProductCategory? category = null,
       string? search = null,
       string? location = null,
       decimal? minPrice = null,
       decimal? maxPrice = null,
       string? sort = null)
        {
            var parameters = new List<string>
            {
                $"number={number}"
            };

            if (category.HasValue)
                parameters.Add($"category={Uri.EscapeDataString(category.Value.ToString())}");

            if (!string.IsNullOrWhiteSpace(search))
                parameters.Add($"q={Uri.EscapeDataString(search)}");

            if (!string.IsNullOrWhiteSpace(location))
                parameters.Add($"lokalizacja={Uri.EscapeDataString(location)}");

            if (minPrice.HasValue)
                parameters.Add($"cenaMin={minPrice.Value.ToString(CultureInfo.InvariantCulture)}");

            if (maxPrice.HasValue)
                parameters.Add($"cenaMax={maxPrice.Value.ToString(CultureInfo.InvariantCulture)}");

            if (!string.IsNullOrWhiteSpace(sort))
                parameters.Add($"sortowanie={Uri.EscapeDataString(sort)}");

            var url = "api/product?" + string.Join("&", parameters);

            var response = await _httpClient.GetAsync(url);

            response.EnsureSuccessStatusCode();

            return await response.Content.ReadFromJsonAsync<List<ProductDTO>>()
                ?? new List<ProductDTO>();
        }


        public async Task<ProductDTO> GetProductByIdAsync(int id)
        {
            var response = await _httpClient.GetAsync($"api/product/{id}");
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<ProductDTO>()
                ?? new ProductDTO();
        }
        public async Task<CompletedStatsDTO> GetCompletedStatsAsync()
        {
            try
            {
                var response = await _httpClient.GetAsync("api/product/stats");

                if (!response.IsSuccessStatusCode)
                    return new CompletedStatsDTO();

                return await response.Content.ReadFromJsonAsync<CompletedStatsDTO>()
                    ?? new CompletedStatsDTO();
            }
            catch
            {
                return new CompletedStatsDTO();
            }
        }
        // ----- Ulubione -----

        // Lista ulubionych ogłoszeń zalogowanego użytkownika (od ostatnio dodanego).
        public async Task<List<ProductDTO>> GetFavoritesAsync()
        {
            try
            {
                using var request = await CreateAuthorizedRequestAsync(HttpMethod.Get, "api/product/favorites");
                if (request == null)
                    return new List<ProductDTO>();

                var response = await _httpClient.SendAsync(request);
                if (!response.IsSuccessStatusCode)
                    return new List<ProductDTO>();

                return await response.Content.ReadFromJsonAsync<List<ProductDTO>>()
                    ?? new List<ProductDTO>();
            }
            catch
            {
                return new List<ProductDTO>();
            }
        }

        // Same identyfikatory ulubionych - do zaznaczania gwiazdek na listach.
        // Zwraca null, gdy nie udało się pobrać (wtedy można spróbować ponownie).
        public async Task<List<int>?> GetFavoriteIdsAsync()
        {
            try
            {
                using var request = await CreateAuthorizedRequestAsync(HttpMethod.Get, "api/product/favorites/ids");
                if (request == null)
                    return new List<int>(); // niezalogowany = brak ulubionych

                var response = await _httpClient.SendAsync(request);
                if (!response.IsSuccessStatusCode)
                    return null;

                return await response.Content.ReadFromJsonAsync<List<int>>()
                    ?? new List<int>();
            }
            catch
            {
                return null;
            }
        }

        // true = dodano do ulubionych, false = usunięto, null = błąd / brak logowania
        public async Task<bool?> ToggleFavoriteAsync(int productId)
        {
            try
            {
                using var request = await CreateAuthorizedRequestAsync(HttpMethod.Post, $"api/product/{productId}/favorite");
                if (request == null)
                    return null;

                var response = await _httpClient.SendAsync(request);
                if (!response.IsSuccessStatusCode)
                    return null;

                var result = await response.Content.ReadFromJsonAsync<FavoriteToggleDTO>();
                return result?.IsFavorite;
            }
            catch
            {
                return null;
            }
        }

        private async Task<HttpRequestMessage?> CreateAuthorizedRequestAsync(HttpMethod method, string url)
        {
            var authState = await _authStateProvider.GetAuthenticationStateAsync();
            var token = authState.User.FindFirst("access_token")?.Value;
            if (string.IsNullOrEmpty(token))
                return null;

            var request = new HttpRequestMessage(method, url);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            return request;
        }

        public async Task<List<LocationDTO>> SearchLocationsAsync(string query)
        {
            var url = $"api/product/searchLocations?query={Uri.EscapeDataString(query)}";

            var response = await _httpClient.GetAsync(url);

            response.EnsureSuccessStatusCode();

            return await response.Content.ReadFromJsonAsync<List<LocationDTO>>()
                ?? new List<LocationDTO>();
        }
    }
}
