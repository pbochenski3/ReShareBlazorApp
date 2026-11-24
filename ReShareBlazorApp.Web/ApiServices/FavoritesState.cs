using Microsoft.AspNetCore.Components.Authorization;

namespace ReShareBlazorApp.Web.ApiServices
{
    // Wspólny stan gwiazdek (ulubionych) dla całej sesji użytkownika.
    // Dzięki temu karta na liście, przycisk na stronie ogłoszenia i licznik w profilu
    // zawsze pokazują to samo i nie odpytują API osobno.
    public class FavoritesState
    {
        private readonly ProductApiService _productApi;
        private readonly AuthenticationStateProvider _authStateProvider;
        private readonly HashSet<int> _ids = new();
        private Task<bool>? _loadTask;

        public FavoritesState(ProductApiService productApi, AuthenticationStateProvider authStateProvider)
        {
            _productApi = productApi;
            _authStateProvider = authStateProvider;
        }

        // Wywoływane po każdej zmianie gwiazdki - komponenty odświeżają wtedy widok.
        public event Action? Changed;

        public int Count => _ids.Count;

        public bool IsFavorite(int productId) => _ids.Contains(productId);

        // Id zalogowanego użytkownika (null = niezalogowany) - do ukrywania gwiazdki przy własnych ogłoszeniach
        public async Task<int?> GetCurrentUserIdAsync()
        {
            var state = await _authStateProvider.GetAuthenticationStateAsync();
            var claim = state.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            return int.TryParse(claim, out var id) ? id : null;
        }

        public async Task<bool> IsLoggedInAsync()
        {
            var state = await _authStateProvider.GetAuthenticationStateAsync();
            return state.User.Identity?.IsAuthenticated == true;
        }

        // Pobiera ulubione z API tylko raz; kolejne wywołania (np. z wielu kart naraz)
        // czekają na ten sam wynik. Po nieudanym pobraniu następne wywołanie spróbuje ponownie.
        public async Task EnsureLoadedAsync()
        {
            var task = _loadTask ??= LoadAsync();
            var loaded = await task;
            if (!loaded && ReferenceEquals(_loadTask, task))
                _loadTask = null;
        }

        private async Task<bool> LoadAsync()
        {
            var ids = await _productApi.GetFavoriteIdsAsync();
            if (ids == null)
                return false;

            _ids.Clear();
            foreach (var id in ids)
                _ids.Add(id);
            return true;
        }

        // Dodaje lub usuwa z ulubionych. Zwraca nowy stan gwiazdki (true = żółta)
        // albo null, gdy się nie udało (brak logowania / błąd API).
        public async Task<bool?> ToggleAsync(int productId)
        {
            await EnsureLoadedAsync();

            var isFavorite = await _productApi.ToggleFavoriteAsync(productId);
            if (isFavorite == null)
                return null;

            if (isFavorite.Value)
                _ids.Add(productId);
            else
                _ids.Remove(productId);

            Changed?.Invoke();
            return isFavorite;
        }
    }
}
