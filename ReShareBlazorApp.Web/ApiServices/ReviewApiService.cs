using System.Net.Http.Json;
using System.Net.Http.Headers;
using Microsoft.AspNetCore.Components.Authorization;
using ReShareBlazorApp.Shared.DTOs;

namespace ReShareBlazorApp.Web.ApiServices
{
    public class ReviewApiService
    {
        private readonly HttpClient _http;
        private readonly AuthenticationStateProvider _auth;

        public ReviewApiService(HttpClient http, AuthenticationStateProvider auth)
        {
            _http = http;
            _auth = auth;
        }

        public async Task<List<ReviewDTO>> GetReviewsForProductAsync(int productId)
        {
            try
            {
                return await _http.GetFromJsonAsync<List<ReviewDTO>>($"api/review/product/{productId}") ?? new();
            }
            catch
            {
                return new();
            }
        }

        public async Task<List<ReviewDTO>> GetReviewsForUserAsync(string username)
        {
            try
            {
                return await _http.GetFromJsonAsync<List<ReviewDTO>>($"api/review/user/{Uri.EscapeDataString(username)}") ?? new();
            }
            catch
            {
                return new();
            }
        }

        public async Task<bool> AddReviewAsync(CreateReviewRequest request)
        {
            var state = await _auth.GetAuthenticationStateAsync();
            var token = state.User.FindFirst("access_token")?.Value;
            if (string.IsNullOrWhiteSpace(token))
                return false;

            using var message = new HttpRequestMessage(HttpMethod.Post, "api/review")
            {
                Content = JsonContent.Create(request)
            };
            message.Headers.Authorization =
                new AuthenticationHeaderValue("Bearer", token);
            using var response = await _http.SendAsync(message);
            return response.IsSuccessStatusCode;
        }
        public async Task<UserRatingDTO> GetUserRatingAsync(int userId)
        {
            try
            {
                return await _http.GetFromJsonAsync<UserRatingDTO>(
                    $"api/review/user/{userId}") ?? new UserRatingDTO();
            }
            catch
            {
                return new UserRatingDTO();
            }
        }
    }
}