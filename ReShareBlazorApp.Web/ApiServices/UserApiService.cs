using Microsoft.AspNetCore.Components.Authorization;

namespace ReShareBlazorApp.Web.ApiServices
{
    public class UserApiService
    {
        private readonly HttpClient _httpClient;
        private readonly AuthenticationStateProvider _authStateProvider;
        public UserApiService(HttpClient httpClient, AuthenticationStateProvider authStateProvider)
        {
            _httpClient = httpClient;
            _authStateProvider = authStateProvider;
        }
        public async Task<bool> CheckIfUserExistsAsync(string username)
        {
            var result = await _httpClient.GetFromJsonAsync<bool>
                ($"api/user?username={Uri.EscapeDataString(username)}");
            return result;
        }

    }
}
