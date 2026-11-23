
using Microsoft.AspNetCore.Components.Authorization;
using ReShareBlazorApp.Api.Date.Enums;
using ReShareBlazorApp.Shared.DTOs;
using ReShareBlazorApp.Shared.Enums;
using ReShareBlazorApp.Web.Models;
using System.Globalization;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

namespace ReShareBlazorApp.Web.ApiServices
{
    public class ProductApiService
    {
        private readonly HttpClient _httpClient;
        private readonly AuthenticationStateProvider _authStateProvider;

        public ProductApiService(
            HttpClient httpClient,
            AuthenticationStateProvider authStateProvider)
        {
            _httpClient = httpClient;
            _authStateProvider = authStateProvider;
        }

        public async Task<(bool Success, string Message)> AddProductAsync(ProductModel productModel)
        {
            var request = await CreateAuthorizedRequestAsync(
                HttpMethod.Post,
                "api/product/addProduct");

            if (request == null)
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
                Images = productModel.Images.Select(image => new ProductImageDTO
                {
                    FileName = image.FileName,
                    ContentType = image.ContentType,
                    Data = image.Data
                }).ToList()
            };

            using (request)
            {
                request.Content = JsonContent.Create(product);

                using var response = await _httpClient.SendAsync(request);

                if (response.IsSuccessStatusCode)
                    return (true, "Produkt dodany pomyślnie!");

                var error = response.StatusCode == HttpStatusCode.BadRequest
                    ? (await response.Content.ReadAsStringAsync()).Trim('"')
                    : response.StatusCode == HttpStatusCode.Forbidden
                        ? await ReadApiMessageAsync(response)
                        : "";

                return (false, string.IsNullOrWhiteSpace(error)
                    ? "Nie udało się dodać produktu!"
                    : error);
            }
        }

        public async Task<string> UpdateProductAsync(int productId, ProductModel productModel)
        {
            var request = await CreateAuthorizedRequestAsync(
                HttpMethod.Put,
                $"api/product/{productId}");

            if (request == null)
                return "Brak tokenu uwierzytelniającego!";

            using (request)
            {
                request.Content = JsonContent.Create(new ProductUpdateDTO
                {
                    Name = productModel.Name,
                    Description = productModel.Description,
                    Price = productModel.Price,
                    ExpiresAt = productModel.ExpiresAt,
                    Category = productModel.Category,
                    City = productModel.City,
                    Type = productModel.Type
                });

                using var response = await _httpClient.SendAsync(request);

                return response.IsSuccessStatusCode
                    ? "Ogłoszenie zostało zaktualizowane!"
                    : "Nie udało się edytować ogłoszenia!";
            }
        }

        public async Task<string> DeleteProductAsync(int productId)
        {
            var request = await CreateAuthorizedRequestAsync(
                HttpMethod.Delete,
                $"api/product/{productId}");

            if (request == null)
                return "Brak tokenu uwierzytelniającego!";

            using (request)
            using (var response = await _httpClient.SendAsync(request))
            {
                return response.IsSuccessStatusCode
                    ? "Ogłoszenie zostało usunięte!"
                    : "Nie udało się usunąć ogłoszenia!";
            }
        }

        public async Task<List<ProductDTO>> GetProductsAsync(
            int number = 5,
            ProductCategory? category = null,
            string? search = null,
            string? location = null,
            decimal? minPrice = null,
            decimal? maxPrice = null,
            string? sort = null,
            int? userId = null,
            ProductType? type = null)
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

            if (userId.HasValue)
                parameters.Add($"userId={userId.Value}");

            if (type.HasValue)
                parameters.Add($"type={Uri.EscapeDataString(type.Value.ToString())}");

            using var response = await _httpClient.GetAsync(
                "api/product?" + string.Join("&", parameters));

            response.EnsureSuccessStatusCode();

            return await response.Content.ReadFromJsonAsync<List<ProductDTO>>()
                ?? new List<ProductDTO>();
        }

        public async Task<ProductDTO?> GetProductByIdAsync(int id)
        {
            using var response = await _httpClient.GetAsync($"api/product/{id}");

            if (response.StatusCode == HttpStatusCode.NotFound)
                return null;

            response.EnsureSuccessStatusCode();

            return await response.Content.ReadFromJsonAsync<ProductDTO>();
        }

        public async Task<List<LocationDTO>> SearchLocationsAsync(string query)
        {
            if (string.IsNullOrWhiteSpace(query))
                return new List<LocationDTO>();

            var url = $"api/product/searchLocations?query={Uri.EscapeDataString(query)}";

            using var response = await _httpClient.GetAsync(url);

            response.EnsureSuccessStatusCode();

            return await response.Content.ReadFromJsonAsync<List<LocationDTO>>()
                ?? new List<LocationDTO>();
        }

        private async Task<HttpRequestMessage?> CreateAuthorizedRequestAsync(
            HttpMethod method,
            string url)
        {
            var state = await _authStateProvider.GetAuthenticationStateAsync();
            var token = state.User.FindFirst("access_token")?.Value;

            if (string.IsNullOrWhiteSpace(token))
                return null;

            var request = new HttpRequestMessage(method, url);

            request.Headers.Authorization =
                new AuthenticationHeaderValue("Bearer", token);

            return request;
        }

        private static async Task<string> ReadApiMessageAsync(HttpResponseMessage response)
        {
            try
            {
                var body = await response.Content.ReadFromJsonAsync<ApiErrorResponse>();
                if (!string.IsNullOrWhiteSpace(body?.Message))
                    return body.Message;
            }
            catch (JsonException)
            {
                // API może zwrócić również zwykły tekst zamiast JSON.
            }

            return "Twoje konto jest zablokowane i nie może dodawać ogłoszeń.";
        }

        private sealed class ApiErrorResponse
        {
            public string? Message { get; set; }
        }
    }
}
