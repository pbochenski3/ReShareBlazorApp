namespace ReShareBlazorApp.Shared
{
    // Kody akcji zapisywane w logach (kolumna ActivityLogs.Action).
    public static class ActivityActions
    {
        public const string UserRegistered = "UserRegistered";
        public const string ProductAdded = "ProductAdded";
        public const string ProductUpdated = "ProductUpdated";
        public const string ProductDeleted = "ProductDeleted";
        public const string ReportAdded = "ReportAdded";
        public const string ReviewAdded = "ReviewAdded";
        public const string UserFollowed = "UserFollowed";
        public const string UserUnfollowed = "UserUnfollowed";
        public const string ProfileUpdated = "ProfileUpdated";
        public const string AvatarChanged = "AvatarChanged";
        public const string AvatarRemoved = "AvatarRemoved";
        // Akcje administracyjne
        public const string AdminUserBlocked = "AdminUserBlocked";
        public const string AdminUserUnblocked = "AdminUserUnblocked";
        public const string AdminProductDeleted = "AdminProductDeleted";
        public const string ReportDeleted = "ReportDeleted";
        public const string ReportedProductDeleted = "ReportedProductDeleted";
        // Logowanie i konto
        public const string UserLoggedIn = "UserLoggedIn";
        public const string UserLoginFailed = "UserLoginFailed";
        public const string UserLoggedOut = "UserLoggedOut";
        public const string PasswordResetRequested = "PasswordResetRequested";
        public const string PasswordChanged = "PasswordChanged";
        // Kolejne akcje administracyjne
        public const string AdminUserCreated = "AdminUserCreated";
        public const string AdminUserUpdated = "AdminUserUpdated";
        public const string AdminRoleChanged = "AdminRoleChanged";
        public const string AdminUserTempBlocked = "AdminUserTempBlocked";
        public const string AdminUserWarned = "AdminUserWarned";
        public const string AdminWarningCleared = "AdminWarningCleared";
        public const string AdminUserDeleted = "AdminUserDeleted";
        public const string AdminReviewHidden = "AdminReviewHidden";
        public const string AdminReviewDeleted = "AdminReviewDeleted";
        public const string AdminLockdownChanged = "AdminLockdownChanged";
        public const string AdminBannerSet = "AdminBannerSet";
        public const string AdminBannerDisabled = "AdminBannerDisabled";
        public const string AdminMessageDeleted = "AdminMessageDeleted";

        public static readonly string[] All =
        {
            UserRegistered, ProductAdded, ProductUpdated, ProductDeleted, ReportAdded,
            ReviewAdded, UserFollowed, UserUnfollowed, ProfileUpdated, AvatarChanged,
            AvatarRemoved, AdminUserBlocked, AdminUserUnblocked, AdminProductDeleted,
            ReportDeleted, ReportedProductDeleted
        };

        public static string Label(string code) => code switch
        {
            UserRegistered => "Rejestracja",
            ProductAdded => "Dodanie ogłoszenia",
            ProductUpdated => "Edycja ogłoszenia",
            ProductDeleted => "Usunięcie ogłoszenia",
            ReportAdded => "Zgłoszenie ogłoszenia",
            ReviewAdded => "Dodanie opinii",
            UserFollowed => "Obserwowanie użytkownika",
            UserUnfollowed => "Przestanie obserwować",
            ProfileUpdated => "Edycja profilu",
            AvatarChanged => "Zmiana zdjęcia profilowego",
            AvatarRemoved => "Usunięcie zdjęcia profilowego",
            AdminUserBlocked => "Zablokowanie użytkownika",
            AdminUserUnblocked => "Odblokowanie użytkownika",
            AdminProductDeleted => "Usunięcie ogłoszenia (admin)",
            ReportDeleted => "Usunięcie zgłoszenia",
            ReportedProductDeleted => "Usunięcie zgłoszonego ogłoszenia",
            UserLoggedIn => "Logowanie",
            UserLoginFailed => "Nieudane logowanie",
            UserLoggedOut => "Wylogowanie",
            PasswordResetRequested => "Prośba o zmianę hasła",
            PasswordChanged => "Zmiana hasła",
            AdminUserCreated => "Utworzenie użytkownika (admin)",
            AdminUserUpdated => "Edycja użytkownika (admin)",
            AdminRoleChanged => "Zmiana roli (admin)",
            AdminUserTempBlocked => "Blokada czasowa użytkownika",
            AdminUserWarned => "Ostrzeżenie użytkownika",
            AdminWarningCleared => "Usunięcie ostrzeżenia",
            AdminUserDeleted => "Usunięcie użytkownika (admin)",
            AdminReviewHidden => "Ukrycie/przywrócenie opinii",
            AdminReviewDeleted => "Usunięcie opinii (admin)",
            AdminLockdownChanged => "Przerwa techniczna",
            AdminBannerSet => "Ustawienie paska promocyjnego",
            AdminBannerDisabled => "Wyłączenie paska promocyjnego",
            AdminMessageDeleted => "Usunięcie wiadomości kontaktowej",
            _ => code
        };
    }
}
