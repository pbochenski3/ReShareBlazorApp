using Microsoft.AspNetCore.Components.Authorization;
using ReShareBlazorApp.Shared.DTOs;
using ReShareBlazorApp.Web.Models;
using System.Net.Http.Headers;

namespace ReShareBlazorApp.Web.ApiServices
{
    public class ReportApiService
    {
        private readonly HttpClient _httpClient;
        private readonly AuthenticationStateProvider _authStateProvider;
        public ReportApiService(HttpClient httpClient, AuthenticationStateProvider authStateProvider)
        {
            _httpClient = httpClient;
            _authStateProvider = authStateProvider;
        }
        private async Task<HttpRequestMessage?> CreateRequestAsync(HttpMethod method, string url)
        {
            var authState = await _authStateProvider.GetAuthenticationStateAsync();
            var token = authState.User.FindFirst("access_token")?.Value;
            if (string.IsNullOrEmpty(token))
                return null;

            var request = new HttpRequestMessage(method, url);
            request.Headers.Authorization =
                new AuthenticationHeaderValue("Bearer", token);
            return request;
        }
        public async Task<bool> AddReportAsync(int productId, ReportModel reportModel)
        {
            if (reportModel.Category == null)
                return false;

            using var request = await CreateRequestAsync(
                HttpMethod.Post,
                "api/report/addReport");
            if (request == null)
                return false;

            var report = new ReportDTO
            {
                ProductId = productId,
                Category = reportModel.Category.Value,
                Description = reportModel.Description.Trim()
            };
            request.Content = JsonContent.Create(report);
            try
            {
                var response = await _httpClient.SendAsync(request);
                return response.IsSuccessStatusCode;
            }
            catch (HttpRequestException)
            {
                return false;
            }
        }
        public async Task<List<ReportDTO>?> GetReportsAsync()
        {
            using var request = await CreateRequestAsync(HttpMethod.Get, "api/report");
            if (request == null)
                return null;
            try
            {
                var response = await _httpClient.SendAsync(request);
                if (!response.IsSuccessStatusCode)
                    return null;
                return await response.Content.ReadFromJsonAsync<List<ReportDTO>>()
                    ?? new List<ReportDTO>();
            }
            catch (HttpRequestException)
            {
                return null;
            }
        }
        public async Task<bool> DeleteReportAsync(int reportId)
        {
            using var request = await CreateRequestAsync(
                HttpMethod.Delete,
                $"api/report/{reportId}");
            if (request == null)
                return false;
            try
            {
                var response = await _httpClient.SendAsync(request);
                return response.IsSuccessStatusCode;
            }
            catch (HttpRequestException)
            {
                return false;
            }
        }
        public async Task<bool> DeleteReportedProductAsync(int reportId)
        {
            using var request = await CreateRequestAsync(
                HttpMethod.Delete,
                $"api/report/{reportId}/product");
            if (request == null)
                return false;
            try
            {
                var response = await _httpClient.SendAsync(request);
                return response.IsSuccessStatusCode;
            }
            catch (HttpRequestException)
            {
                return false;
            }
        }

        public async Task<AdminReportThreadsDTO?> GetReportThreadsAsync(int reportId)
        {
            using var request = await CreateRequestAsync(HttpMethod.Get, $"api/report/{reportId}/threads");
            if (request == null)
                return null;
            try
            {
                var response = await _httpClient.SendAsync(request);
                if (!response.IsSuccessStatusCode)
                    return null;
                return await response.Content.ReadFromJsonAsync<AdminReportThreadsDTO>();
            }
            catch (HttpRequestException)
            {
                return null;
            }
        }

        public async Task<AdminReplyResultDTO> ReplyToReportAsync(int reportId, AdminReportReplyRequest replyRequest)
        {
            using var request = await CreateRequestAsync(HttpMethod.Post, $"api/report/{reportId}/reply");
            if (request == null)
                return new AdminReplyResultDTO { Success = false, ErrorMessage = "Brak autoryzacji." };

            request.Content = JsonContent.Create(replyRequest);
            try
            {
                var response = await _httpClient.SendAsync(request);
                var result = await response.Content.ReadFromJsonAsync<AdminReplyResultDTO>();
                return result ?? new AdminReplyResultDTO
                {
                    Success = response.IsSuccessStatusCode,
                    ErrorMessage = response.IsSuccessStatusCode ? null : "Nie udało się wysłać wiadomości."
                };
            }
            catch (HttpRequestException)
            {
                return new AdminReplyResultDTO { Success = false, ErrorMessage = "Błąd połączenia z API." };
            }
        }
    }
}
