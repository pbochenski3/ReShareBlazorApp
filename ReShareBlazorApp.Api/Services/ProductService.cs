
using ReShareBlazorApp.Api.Date;
using ReShareBlazorApp.Api.Date.Enums;
using ReShareBlazorApp.Api.Interfaces.Repositories;
using ReShareBlazorApp.Api.Interfaces.Services;
using ReShareBlazorApp.Shared.DTOs;

namespace ReShareBlazorApp.Api.Services
{
    public class ProductService : IProductService
    {
        private const string ImageFolder = "products";
        public const int MaxImagesPerProduct = 5;
        public const int MaxImageSizeBytes = 3 * 1024 * 1024;

        private readonly IProductRepository _productRepository;
        private readonly IImageStorage _imageStorage;
        public ProductService(IProductRepository productRepository, IImageStorage imageStorage)
        {
            _productRepository = productRepository;
            _imageStorage = imageStorage;
        }
        public async Task<bool> AddProductAsync(ProductDTO product)
        {
            var images = new List<ProductImage>();
            var savedFiles = new List<string>();
            try
            {
                foreach (var (image, index) in (product.Images ?? new()).Select((image, index) => (image, index)))
                {
                    var contentType = DetectContentType(image.Data);
                    if (contentType == null)
                    {
                        DeleteFiles(savedFiles);
                        return false;
                    }

                    var storedFileName = await _imageStorage.SaveAsync(image.Data, GetExtension(contentType), ImageFolder);
                    savedFiles.Add(storedFileName);
                    images.Add(new ProductImage
                    {
                        FileName = TrimFileName(image.FileName),
                        ContentType = contentType,
                        StoredFileName = storedFileName,
                        SortOrder = index
                    });
                }

                var added = await AddProductToDatabaseAsync(product, images);
                if (!added)
                    DeleteFiles(savedFiles);
                return added;
            }
            catch
            {
                DeleteFiles(savedFiles);
                throw;
            }
        }
        private async Task<bool> AddProductToDatabaseAsync(ProductDTO product, List<ProductImage> images)
        {
            var productModel = new Product
            {
                Name = product.Name,
                Description = product.Description,
                Price = product.Price,
                AddedAt = product.AddedAt,
                ExpiresAt = product.ExpiresAt,
                Category = product.Category,
                Type = product.Type,
                City = product.City,
                UserId = product.UserId,
                Images = images
            };
            var result = await _productRepository.AddProductAsync(productModel);
            return result;
        }

        public async Task<bool> UpdateProductAsync(int productId, int userId, ProductUpdateDTO product)
        {
            return await _productRepository.UpdateProductAsync(productId, userId, product);
        }

        public async Task<bool> DeleteProductAsync(int productId, int userId)
        {
            var product = await _productRepository.GetProductById(productId);
            if (product == null || product.UserId != userId)
                return false;

            var deleted = await _productRepository.DeleteProductAsync(productId, userId);
            if (deleted)
                DeleteFiles(product.Images.Select(image => image.StoredFileName));

            return deleted;
        }

        public async Task<List<ProductDTO>> GetProductsAsync
            (int number = 5,
            ProductCategory? category = null)
        {
            var listOfProducts = await _productRepository.GetProducts(number, category);
            var imageIds = await _productRepository
                .GetImageIdsAsync(listOfProducts.Select(p => p.Id));
            var productsDtoList = listOfProducts.
                Select(p => new ProductDTO
                {
                    Name = p.Name,
                    Description = p.Description,
                    ProductId = p.Id,
                    Price = p.Price,
                    AddedAt = p.AddedAt,
                    ExpiresAt = p.ExpiresAt,
                    Category = p.Category,
                    City = p.City,
                    Type = p.Type,
                    UserId = p.UserId,
                    ImageIds = imageIds.GetValueOrDefault(p.Id) ?? new List<int>()
                })
                .ToList();
            return productsDtoList;
        }
        public async Task<ProductDTO?> GetProductById(int id)
        {
            var product = await _productRepository.GetProductById(id);
            if (product == null)
                return null;

            var imageIds = await _productRepository.GetImageIdsAsync(new[] { id });
            return new ProductDTO
            {
                ImageIds = imageIds.GetValueOrDefault(id) ?? new List<int>(),
                ProductId = product.Id,
                UserId = product.UserId,
                Name = product.Name,
                Description = product.Description,
                Price = product.Price,
                City = product.City,
                Category = product.Category,
                AddedAt = product.AddedAt,
                ExpiresAt = product.ExpiresAt,
                Type = product.Type,
                AddedBy = product.user.UserName ?? "",
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

            var data = await _imageStorage.ReadAsync(image.StoredFileName, ImageFolder);
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
        /// <summary>Zwraca komunikat błędu albo null, gdy zdjęcia są poprawne.</summary>
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
                _imageStorage.Delete(storedFileName, ImageFolder);
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
            return name.Length > 255 ? name[..255] : name;
        }
        // Rozpoznanie formatu po nagłówku pliku (JPEG / PNG / WEBP).
        private static string? DetectContentType(byte[] data)
        {
            if (data == null || data.Length < 12)
                return null;

            if (data[0] == 0xFF && data[1] == 0xD8 && data[2] == 0xFF)
                return "image/jpeg";

            if (data[0] == 0x89 && data[1] == 0x50 && data[2] == 0x4E && data[3] == 0x47)
                return "image/png";

            if (data[0] == 'R' && data[1] == 'I' && data[2] == 'F' && data[3] == 'F' &&
                data[8] == 'W' && data[9] == 'E' && data[10] == 'B' && data[11] == 'P')
                return "image/webp";

            return null;
        }
    }
}
