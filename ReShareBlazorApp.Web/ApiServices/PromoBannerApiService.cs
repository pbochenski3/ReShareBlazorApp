using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.JSInterop;
using ReShareBlazorApp.Shared.DTOs;

namespace ReShareBlazorApp.Web.ApiServices
{
	public class PromoBannerApiService
	{
		private readonly HttpClient _httpClient;
		private readonly AuthenticationStateProvider _authStateProvider;
		private readonly IJSRuntime _js;

		public PromoBannerApiService(
			HttpClient httpClient,
			AuthenticationStateProvider authStateProvider,
			IJSRuntime js)
		{
			_httpClient = httpClient;
			_authStateProvider = authStateProvider;
			_js = js;
		}

		private async Task<string?> GetTokenAsync()
		{
			try
			{
				var authState = await _authStateProvider.GetAuthenticationStateAsync();
				var token = authState.User.FindFirst("access_token")?.Value;
				if (!string.IsNullOrWhiteSpace(token))
					return token.Trim('"');
			}
			catch
			{
			}

			try
			{
				var keys = new[] { "access_token", "authToken", "token", "jwt", "jwtToken", "accessToken" };
				foreach (var key in keys)
				{
					var stored = await _js.InvokeAsync<string?>("localStorage.getItem", key)
							  ?? await _js.InvokeAsync<string?>("sessionStorage.getItem", key);
					if (!string.IsNullOrWhiteSpace(stored))
						return stored.Trim('"');
				}
			}
			catch
			{
			}

			return null;
		}

		public async Task<PromoBannerDTO?> GetActiveBannerAsync()
		{
			try
			{
				var response = await _httpClient.GetAsync("api/promobanner/active");
				if (!response.IsSuccessStatusCode)
					return null;

				var content = await response.Content.ReadAsStringAsync();
				if (string.IsNullOrWhiteSpace(content) || content.Trim().Equals("null", StringComparison.OrdinalIgnoreCase))
					return null;

				return JsonSerializer.Deserialize<PromoBannerDTO>(
					content,
					new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
			}
			catch
			{
				return null;
			}
		}

		public async Task<(bool Ok, string Message)> SetBannerAsync(CreatePromoBannerRequest dto)
		{
			var token = await GetTokenAsync();
			if (string.IsNullOrWhiteSpace(token))
				return (false, "Brak tokenu uwierzytelniającego.");

			using var request = new HttpRequestMessage(HttpMethod.Post, "api/promobanner");
			request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
			request.Content = JsonContent.Create(dto);

			var response = await _httpClient.SendAsync(request);
			var body = await response.Content.ReadAsStringAsync();

			if (!response.IsSuccessStatusCode)
				return (false, string.IsNullOrWhiteSpace(body) ? $"Błąd ({(int)response.StatusCode})" : body);

			return (true, "Zapisano pasek promocyjny!");
		}

		public async Task<(bool Ok, string Message)> DisableBannerAsync()
		{
			var token = await GetTokenAsync();
			if (string.IsNullOrWhiteSpace(token))
				return (false, "Brak tokenu uwierzytelniającego.");

			using var request = new HttpRequestMessage(HttpMethod.Post, "api/promobanner/disable");
			request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

			var response = await _httpClient.SendAsync(request);
			if (!response.IsSuccessStatusCode)
				return (false, "Nie udało się wyłączyć paska.");

			return (true, "Wyłączono pasek promocyjny.");
		}
	}
}