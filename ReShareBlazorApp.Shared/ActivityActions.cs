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
            _ => code
        };
    }
}
