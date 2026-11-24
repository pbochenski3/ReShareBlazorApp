using System.Net;

namespace ReShareBlazorApp.Api.Services;

public static class EmailStyle
{
    public static string ResetPassword(string? userName, string resetLink)
    {
        var name = WebUtility.HtmlEncode(string.IsNullOrWhiteSpace(userName) ? "Użytkowniku" : userName);
        var link = WebUtility.HtmlEncode(resetLink);

        return $@"
<!DOCTYPE html>
<html lang=""pl"">
<head>
    <meta charset=""UTF-8"">
</head>
<body style=""margin:0;padding:24px;background:#f1f5f9;font-family:Arial,Helvetica,sans-serif;color:#1e293b;"">
    <div style=""max-width:520px;margin:0 auto;background:#ffffff;border-radius:12px;overflow:hidden;border:1px solid #e2e8f0;"">
        <div style=""background:#2f6fed;padding:24px;text-align:center;color:#ffffff;"">
            <h1 style=""margin:0;font-size:22px;"">ODDAJ DALEJ</h1>
            <p style=""margin:6px 0 0;font-size:13px;opacity:0.9;"">Zmiana hasła do konta</p>
        </div>

        <div style=""padding:28px;"">
            <p style=""margin:0 0 14px;font-size:15px;"">Cześć <strong>{name}</strong>,</p>
            <p style=""margin:0 0 22px;font-size:15px;line-height:1.5;color:#475569;"">
                Otrzymaliśmy prośbę o zmianę hasła do Twojego konta. Kliknij przycisk poniżej, aby ustawić nowe hasło:
            </p>

            <div style=""text-align:center;margin:24px 0;"">
                <a href=""{link}"" style=""display:inline-block;padding:13px 26px;background:#2f6fed;color:#ffffff;text-decoration:none;font-weight:bold;border-radius:8px;font-size:15px;"">
                    Zmień hasło
                </a>
            </div>

            <p style=""margin:0 0 12px;font-size:13px;line-height:1.5;color:#64748b;"">
                Link jest ważny przez 1 godzinę. Jeśli to nie Ty prosiłeś o zmianę hasła, zignoruj tę wiadomość.
            </p>

            <p style=""margin:18px 0 6px;font-size:12px;color:#64748b;"">Gdyby przycisk nie działał, skopiuj link:</p>
            <p style=""margin:0;font-size:12px;word-break:break-all;"">
                <a href=""{link}"" style=""color:#2f6fed;"">{link}</a>
            </p>
        </div>
    </div>
</body>
</html>";
    }
}