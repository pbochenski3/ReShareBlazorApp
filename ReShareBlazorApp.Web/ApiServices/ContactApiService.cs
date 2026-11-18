using System.Net.Http.Json;
using ReShareBlazorApp.Shared.DTOs;

namespace ReShareBlazorApp.Web.ApiServices
{
    public class ContactApiService
    {
        private readonly HttpClient _httpClient;

        public ContactApiService(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public async Task<bool> SendContactMessageAsync(ContactRequest request)
        {
            var response = await _httpClient.PostAsJsonAsync("api/contact", request);
            return response.IsSuccessStatusCode;
        }
    }
}