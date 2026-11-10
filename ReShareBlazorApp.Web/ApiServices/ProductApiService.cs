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
                AddedAt = productModel.AddedAt,
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
    }
}
