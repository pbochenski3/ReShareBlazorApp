using Microsoft.Extensions.Logging;
using ReShareBlazorApp.Api.Date;
using ReShareBlazorApp.Api.Date.Enums;
using ReShareBlazorApp.Api.Interfaces.Repositories;
using ReShareBlazorApp.Api.Interfaces.Services;
using ReShareBlazorApp.Shared.DTOs;
using ReShareBlazorApp.Shared.Enums;

namespace ReShareBlazorApp.Api.Services
{
    public class ProductService : IProductService
    {
        private const string ImageFolder = "products";

        public const int MaxImagesPerProduct = 5;
        public const int MaxImageSizeBytes = 3 * 1024 * 1024;

        private readonly IProductRepository _productRepository;
        private readonly IImageStorage _imageStorage;
        private readonly INotificationService _notificationService;
        private readonly IBadgeService _badgeService;
        private readonly ILogger<ProductService> _logger;

        public ProductService(
            IProductRepository productRepository,
            IImageStorage imageStorage,
            INotificationService notificationService,
            IBadgeService badgeService,
            ILogger<ProductService> logger)
        {
            _productRepository = productRepository;
            _imageStorage = imageStorage;
            _notificationService = notificationService;
            _badgeService = badgeService;
            _logger = logger;
        }

        public async Task<int?> AddProductAsync(ProductDTO product)
        {
            if (product == null ||
                string.IsNullOrWhiteSpace(product.Name) ||
                product.Name.Trim().Length is < 3 or > 50 ||
                string.IsNullOrWhiteSpace(product.Description) ||
                product.Description.Trim().Length > 5000 ||
                string.IsNullOrWhiteSpace(product.City) ||
                product.City.Trim().Length > 60 ||
                 product.PriceMin < 0.01m ||
                 product.PriceMax < product.PriceMin ||
                 product.PriceMax > 100000 ||
                !Enum.IsDefined(product.Type) ||
                !Enum.IsDefined(product.Category) ||
                product.ExpiresAt <= DateTime.UtcNow)
            {
                return null;
            }

            var images = new List<ProductImage>();
            var savedFiles = new List<string>();

            var validationError = ValidateImages(product.Images);

            if (validationError != null)
                return null;

            try
            {
                foreach (var (image, index) in
                    (product.Images ?? new()).Select((image, index) => (image, index)))
                {
                    var contentType = DetectContentType(image.Data);

                    if (contentType == null)
                        return null;

                    var storedFileName = await _imageStorage.SaveAsync(
                        image.Data,
                        GetExtension(contentType),
                        ImageFolder);

                    savedFiles.Add(storedFileName);

                    images.Add(new ProductImage
                    {
                        FileName = TrimFileName(image.FileName),
                        ContentType = contentType,
                        StoredFileName = storedFileName,
                        SortOrder = index
                    });
                }

                var productModel = new Product
                {
                    Name = product.Name,
                    Description = product.Description,
                    Price = product.PriceMin,
                    PriceMin = product.PriceMin,
                    PriceMax = product.PriceMax,
                    AddedAt = product.AddedAt,
                    ExpiresAt = product.ExpiresAt,
                    Category = product.Category,
                    Condition = product.Condition,
                    Type = product.Type,
                    City = product.City,
                    UserId = product.UserId,
                    Images = images
                };

                var result = await _productRepository.AddProductAsync(productModel);

                if (!result)
                    DeleteFiles(savedFiles);
                else
                {
                    try
                    {
                        if (productModel.Type == ProductType.LookingFor)
                        {
                            await _notificationService.CreateForMatchingOffersAsync(productModel);
                        }
                        else
                        {
                            await _notificationService.CreateForMatchingWantedListingsAsync(productModel);
                        }
                    }
                    catch (Exception exception)
                    {
                        _logger.LogError(exception,
                            "Nie udało się utworzyć powiadomień dla nowego ogłoszenia {ProductId}",
                            productModel.Id);
                    }

                    await AwardProductBadgesAsync(productModel.UserId);
                }

                return result ? productModel.Id : null;
            }
            catch
            {
                DeleteFiles(savedFiles);
                throw;
            }
        }

        private async Task AwardProductBadgesAsync(int userId)
        {
            try
            {
                var productCount = await _productRepository.CountUserProductsAsync(userId);
                var earnedBadges = await _badgeService.GetUserBadgesAsync(userId);

                if (productCount == 1)
                {
                    await AwardBadgeAndNotifyAsync(userId, "Pierwsze ogłoszenie", earnedBadges);
                }

                if (productCount >= 5)
                {
                    await AwardBadgeAndNotifyAsync(userId, "Aktywny sprzedawca", earnedBadges);
                }
            }
            catch (Exception exception)
            {
                _logger.LogError(exception,
                    "Nie udało się przyznać odznak za ogłoszenia użytkownikowi {UserId}", userId);
            }
        }

        private async Task AwardBadgeAndNotifyAsync(
            int userId,
            string badgeName,
            List<ReShareBlazorApp.Shared.DTOs.BadgeDTO> earnedBadges)
        {
            var badge = earnedBadges.FirstOrDefault(item =>
                string.Equals(item.Name, badgeName, StringComparison.OrdinalIgnoreCase));

            if (badge == null || badge.IsEarned)
                return;

            if (!await _badgeService.AwardBadgeAsync(userId, badgeName))
                return;

            try
            {
                await _notificationService.CreateAsync(
                    userId,
                    NotificationType.BadgeEarned,
                    $"Zdobyto nową odznakę: {badge.Name}",
                    badge.Description);
            }
            catch (Exception exception)
            {
                _logger.LogError(exception,
                    "Nie udało się utworzyć powiadomienia o odznace {BadgeName} dla użytkownika {UserId}",
                    badgeName,
                    userId);
            }
        }

        public Task<bool?> ToggleFavoriteAsync(int userId, int productId)
        {
            return _productRepository.ToggleFavoriteAsync(userId, productId);
        }

        public Task<List<int>> GetFavoriteIdsAsync(int userId)
        {
            return _productRepository.GetFavoriteProductIdsAsync(userId);
        }

        public async Task<List<ProductDTO>> GetFavoritesAsync(int userId)
        {
            var products = await _productRepository.GetFavoriteProductsAsync(userId);

            var imageIds = await _productRepository.GetImageIdsAsync(
                products.Select(p => p.Id));

            return products.Select(p => new ProductDTO
            {
                Name = p.Name,
                Description = p.Description,
                ProductId = p.Id,
                 Price = p.Price,
                 PriceMin = p.PriceMin,
                 PriceMax = p.PriceMax,
                AddedAt = p.AddedAt,
                ExpiresAt = p.ExpiresAt,
                Category = p.Category,
                City = p.City,
                Type = p.Type,
                UserId = p.UserId,
                ImageIds = imageIds.GetValueOrDefault(p.Id) ?? new List<int>()
            }).ToList();
        }

        public async Task<List<ProductDTO>> GetProductsAsync(
            int number = 5,
            ProductCategory? category = null,
            string? search = null,
            string? location = null,
            decimal? minPrice = null,
            decimal? maxPrice = null,
            string? sort = null,
            int? userId = null,
            ProductType? type = null)
        {
            var products = await _productRepository.GetProducts(
                number,
                category,
                search,
                location,
                minPrice,
                maxPrice,
                sort,
                userId,
                type
            );

            var imageIds = await _productRepository.GetImageIdsAsync(
                products.Select(p => p.Id));

            return products.Select(p => new ProductDTO
            {
                Name = p.Name,
                Description = p.Description,
                ProductId = p.Id,
                 Price = p.Price,
                 PriceMin = p.PriceMin,
                 PriceMax = p.PriceMax,
                AddedAt = p.AddedAt,
                ExpiresAt = p.ExpiresAt,
                Category = p.Category,
                Condition = p.Condition,
                City = p.City,
                Type = p.Type,
                UserId = p.UserId,
                ImageIds = imageIds.GetValueOrDefault(p.Id) ?? new List<int>()
            }).ToList();
        }

        public async Task<bool> UpdateProductAsync(
            int productId,
            int userId,
            ProductUpdateDTO product)
        {
            if (product == null ||
                string.IsNullOrWhiteSpace(product.Name) ||
                product.Name.Trim().Length is < 3 or > 50 ||
                string.IsNullOrWhiteSpace(product.Description) ||
                product.Description.Trim().Length > 5000 ||
                string.IsNullOrWhiteSpace(product.City) ||
                product.City.Trim().Length > 60 ||
                 product.PriceMin < 0.01m ||
                 product.PriceMax < product.PriceMin ||
                 product.PriceMax > 100000 ||
                !Enum.IsDefined(product.Type) ||
                !Enum.IsDefined(product.Category) ||
                product.ExpiresAt <= DateTime.UtcNow)
            {
                return false;
            }

            return await _productRepository.UpdateProductAsync(
                productId,
                userId,
                product);
        }

        public async Task<bool> DeleteProductAsync(int productId, int userId)
        {
            var product = await _productRepository.GetProductById(productId);

            if (product == null || product.UserId != userId)
                return false;

            var deleted = await _productRepository.DeleteProductAsync(
                productId,
                userId);

            if (deleted)
            {
                DeleteFiles(product.Images.Select(image => image.StoredFileName));
            }

            return deleted;
        }

        public async Task<bool> MarkAsSoldAsync(int productId, int userId)
        {
            var product = await _productRepository.GetProductById(productId);

            if (product == null || product.UserId != userId)
                return false;

            var marked = await _productRepository.MarkAsSoldAsync(productId, userId);

            if (marked)
            {
                DeleteFiles(product.Images.Select(image => image.StoredFileName));
            }

            return marked;
        }

        public async Task<CompletedStatsDTO> GetCompletedStatsAsync()
        {
            var counts = await _productRepository.GetCompletedCountsByCategoryAsync();

            var stats = new CompletedStatsDTO
            {
                TotalCompleted = counts.Values.Sum(),
                TopCategory = ProductCategory.Tickets,
                TopCategoryCount = counts.GetValueOrDefault(ProductCategory.Tickets)
            };

            foreach (var (category, count) in counts.OrderBy(c => c.Key))
            {
                if (count > stats.TopCategoryCount)
                {
                    stats.TopCategory = category;
                    stats.TopCategoryCount = count;
                }
            }

            return stats;
        }

        public async Task<ProductDTO?> GetProductById(int id)
        {
            var product = await _productRepository.GetProductById(id);

            if (product == null)
                return null;

            return new ProductDTO
            {
                ImageIds = product.Images
                    .OrderBy(i => i.SortOrder)
                    .ThenBy(i => i.Id)
                    .Select(i => i.Id)
                    .ToList(),

                ProductId = product.Id,
                UserId = product.UserId,
                Name = product.Name,
                Description = product.Description,
                 Price = product.PriceMin,
                 PriceMin = product.PriceMin,
                 PriceMax = product.PriceMax,
                City = product.City,
                Category = product.Category,
                Condition = product.Condition,
                AddedAt = product.AddedAt,
                ExpiresAt = product.ExpiresAt,
                Type = product.Type,
                AddedBy = product.user?.UserName ?? ""
            };
        }

        public async Task<List<LocationDTO>> GetLocationsAsync(string query)
        {
            query = query.Trim();

            return await _productRepository.GetLocationsAsync(query);
        }

        public async Task<ProductImageDTO?> GetImageAsync(int imageId)
        {
            var image = await _productRepository.GetImageAsync(imageId);

            if (image == null)
                return null;

            var data = await _imageStorage.ReadAsync(
                image.StoredFileName,
                ImageFolder);

            if (data == null)
                return null;

            return new ProductImageDTO
            {
                Id = image.Id,
                FileName = image.FileName,
                ContentType = image.ContentType,
                Data = data
            };
        }

        public string? ValidateImages(IReadOnlyList<ProductImageDTO>? images)
        {
            if (images == null || images.Count == 0)
                return null;

            if (images.Count > MaxImagesPerProduct)
                return $"Możesz dodać maksymalnie {MaxImagesPerProduct} zdjęć.";

            foreach (var image in images)
            {
                if (image.Data == null || image.Data.Length == 0)
                    return "Jedno ze zdjęć jest puste.";

                if (image.Data.Length > MaxImageSizeBytes)
                    return $"Zdjęcie jest za duże (maksymalnie {MaxImageSizeBytes / 1024 / 1024} MB).";

                if (DetectContentType(image.Data) == null)
                    return "Dozwolone formaty zdjęć to JPG, PNG i WEBP.";
            }

            return null;
        }

        private void DeleteFiles(IEnumerable<string> storedFileNames)
        {
            foreach (var storedFileName in storedFileNames)
            {
                _imageStorage.Delete(storedFileName, ImageFolder);
            }
        }

        private static string GetExtension(string contentType) => contentType switch
        {
            "image/png" => ".png",
            "image/webp" => ".webp",
            _ => ".jpg"
        };

        private static string TrimFileName(string? fileName)
        {
            var name = Path.GetFileName(fileName ?? "");

            return name.Length > 255
                ? name[..255]
                : name;
        }

        private static string? DetectContentType(byte[] data)
        {
            if (data == null || data.Length < 12)
                return null;

            if (data[0] == 0xFF &&
                data[1] == 0xD8 &&
                data[2] == 0xFF)
            {
                return "image/jpeg";
            }

            if (data[0] == 0x89 &&
                data[1] == 0x50 &&
                data[2] == 0x4E &&
                data[3] == 0x47)
            {
                return "image/png";
            }

            if (data[0] == 'R' &&
                data[1] == 'I' &&
                data[2] == 'F' &&
                data[3] == 'F' &&
                data[8] == 'W' &&
                data[9] == 'E' &&
                data[10] == 'B' &&
                data[11] == 'P')
            {
                return "image/webp";
            }

            return null;
        }
    }
}