using System.Globalization;

namespace ReShareBlazorApp.Shared.Categories
{
    // Wspólne formatowanie ceny, żeby wszędzie wyglądało tak samo
    public static class ListingFormat
    {
        private static readonly CultureInfo Pl = CreateCulture();

        // gdyby na serwerze nie było polskiej kultury (tryb invariant), używamy kultury domyślnej
        private static CultureInfo CreateCulture()
        {
            try { return CultureInfo.GetCultureInfo("pl-PL"); }
            catch (CultureNotFoundException) { return CultureInfo.InvariantCulture; }
        }

        public static string Price(decimal price)
        {
            if (price <= 0) return "Bezpłatnie";                       // 0 zł = oddam
            var format = price % 1 == 0 ? "N0" : "N2";                 // bez groszy, jeśli cena okrągła
            return price.ToString(format, Pl) + " zł";
        }
    }
}
