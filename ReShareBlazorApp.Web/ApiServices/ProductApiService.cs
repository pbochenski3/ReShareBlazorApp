using Microsoft.AspNetCore.Components.Authorization;
using ReShareBlazorApp.Api.Date.Enums;
using ReShareBlazorApp.Shared.DTOs;
using ReShareBlazorApp.Web.Models;
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
        public async Task<string> AddProductAsync(ProductModel productModel)
        {
            var authState = await _authStateProvider.GetAuthenticationStateAsync();
            var token = authState.User.FindFirst("access_token")?.Value;
            if (string.IsNullOrEmpty(token))
                return "Brak tokenu uwierzytelniającego!";

            var product = new ProductDTO
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
                HttpMethod.Post,
                "api/product/addProduct"
                );
            request.Headers.Authorization =
                new AuthenticationHeaderValue("Bearer", token);
            request.Content = JsonContent.Create(product);
            var response = await _httpClient.SendAsync(request);
            if (!response.IsSuccessStatusCode)
                return "Nie udało się dodać produkut!";
            return "Produkt dodany pomyślnie!";
        }
        public async Task<List<ProductDTO>> GetProductsAsync
            (int number = 5,
            ProductCategory? category = null)
        {
            var url = $"api/product?number={number}";
            if(category.HasValue)
            {
                url += $"&category={Uri.EscapeDataString(category.Value.ToString())}";
            }
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
