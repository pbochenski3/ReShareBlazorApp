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
                Price = productModel.Price,
                Description = productModel.Description,
                ExpiresAt = productModel.ExpiresAt,
                Category = productModel.Category,
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
        // Ogłoszenia zalogowanego użytkownika (zakładka "Moje ogłoszenia" w profilu)
        public async Task<List<ProductDTO>> GetMyProductsAsync()
        {
            var authState = await _authStateProvider.GetAuthenticationStateAsync();
            var token = authState.User.FindFirst("access_token")?.Value;
            if (string.IsNullOrEmpty(token))
                return new List<ProductDTO>();
            using var request = new HttpRequestMessage(HttpMethod.Get, "api/product/mine");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            var response = await _httpClient.SendAsync(request);
            if (!response.IsSuccessStatusCode)
                return new List<ProductDTO>();
            return await response.Content.ReadFromJsonAsync<List<ProductDTO>>() ?? new List<ProductDTO>();
        }
        public async Task<ProductDetailsDTO?> GetProductByIdAsync(int id)
        {
            var response = await _httpClient.GetAsync($"api/product/{id}");
            if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
                return null;
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<ProductDetailsDTO>();
        }
        public async Task<List<ProductDTO>> GetProductsAsync
            (int number = 5,
            IEnumerable<ProductCategory>? categories = null,
            ProductSort sort = ProductSort.Newest)
        {
            var url = $"api/product?number={number}&sort={sort}";
            if (categories != null)
            {
                foreach (var category in categories)
                    url += $"&categories={Uri.EscapeDataString(category.ToString())}";
            }
            var response = await _httpClient.GetAsync(url);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<List<ProductDTO>>()
                ?? new List<ProductDTO>();
        }
    }
}
