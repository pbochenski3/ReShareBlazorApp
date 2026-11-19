using System.Net.Http.Json;
using ReShareBlazorApp.Shared.DTOs;

namespace ReShareBlazorApp.Web.ApiServices
{
    public class ReviewApiService
    {
        private readonly HttpClient _http;

        public ReviewApiService(HttpClient http)
        {
            _http = http;
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

        public async Task<bool> AddReviewAsync(CreateReviewRequest request)
        {
            var response = await _http.PostAsJsonAsync("api/review", request);
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