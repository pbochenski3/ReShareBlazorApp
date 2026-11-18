
using ReShareBlazorApp.Api.Date;
using ReShareBlazorApp.Api.Date.Enums;
using ReShareBlazorApp.Api.Interfaces.Repositories;
using ReShareBlazorApp.Api.Interfaces.Services;
using ReShareBlazorApp.Shared.DTOs;

namespace ReShareBlazorApp.Api.Services
{
    public class ProductService : IProductService
    {
        private readonly IProductRepository _productRepository;
        public ProductService(IProductRepository productRepository)
        {
            _productRepository = productRepository;
        }
        public async Task<bool> AddProductAsync(ProductDTO product)
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
                UserId = product.UserId
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
            return await _productRepository.DeleteProductAsync(productId, userId);
        }

        public async Task<List<ProductDTO>> GetProductsAsync
            (int number = 5,
            ProductCategory? category = null)
        {
            var listOfProducts = await _productRepository.GetProducts(number, category);
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
                })
                .ToList();
            return productsDtoList;
        }
        public async Task<ProductDTO> GetProductById(int id)
        {
            var product = await _productRepository.GetProductById(id);
            return new ProductDTO
            {
                ProductId = product.Id,
                Name = product.Name,
                Description = product.Description,
                Price = product.Price,
                City = product.City,
                Category = product.Category,
                AddedAt = product.AddedAt,
                ExpiresAt = product.ExpiresAt,
                Type = product.Type,
                UserId = product.UserId,
                AddedBy = product.user.UserName ?? "",
            };
        }
        public async Task<List<LocationDTO>> GetLocationsAsync(string query)
        {
            query = query.Trim();
            return await _productRepository.GetLocationsAsync(query);
        }
    }
}
