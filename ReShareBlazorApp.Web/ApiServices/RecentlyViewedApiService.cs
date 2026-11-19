
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Components.Authorization;
using ReShareBlazorApp.Shared.DTOs;

namespace ReShareBlazorApp.Web.ApiServices;

public class RecentlyViewedApiService
{
	private readonly HttpClient _http;
	private readonly AuthenticationStateProvider _auth;

	public RecentlyViewedApiService(
		HttpClient http,
		AuthenticationStateProvider auth)
	{
		_http = http;
		_auth = auth;
	}

	private async Task<HttpRequestMessage?> CreateRequestAsync(
		HttpMethod method,
		string url)
	{
		var state = await _auth.GetAuthenticationStateAsync();

		var token = state.User.FindFirst("access_token")?.Value;

		if (string.IsNullOrWhiteSpace(token))
			return null;

		var request = new HttpRequestMessage(method, url);

		request.Headers.Authorization =
			new AuthenticationHeaderValue("Bearer", token);

		return request;
	}

	public async Task<bool> RecordViewAsync(int productId)
	{
		using var request = await CreateRequestAsync(
			HttpMethod.Post,
			$"api/recently-viewed/{productId}");

		if (request == null)
			return false;

		using var response = await _http.SendAsync(request);

		return response.IsSuccessStatusCode;
	}

	public async Task<List<ProductDTO>> GetRecentlyViewedAsync()
	{
		using var request = await CreateRequestAsync(
			HttpMethod.Get,
			"api/recently-viewed");

		if (request == null)
			return new List<ProductDTO>();

		using var response = await _http.SendAsync(request);

		if (!response.IsSuccessStatusCode)
			return new List<ProductDTO>();

		return await response.Content.ReadFromJsonAsync<List<ProductDTO>>()
			?? new List<ProductDTO>();
	}
}
