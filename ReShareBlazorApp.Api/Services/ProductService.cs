
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
                Price = product.Price,
                Description = product.Description,
                AddedAt = product.AddedAt,
                ExpiresAt = product.ExpiresAt,
                Category = product.Category,
                UserId = product.UserId
            };
            var result = await _productRepository.AddProductAsync(productModel);
            return result;
        }
        public async Task<List<ProductDTO>> GetProductsAsync
            (int number = 5,
            List<ProductCategory>? categories = null,
            ProductSort sort = ProductSort.Newest)
        {
            var listOfProducts = await _productRepository.GetProducts(number, categories, sort);
            var productsDtoList = listOfProducts.
                Select(p => new ProductDTO
                {
                    Id = p.Id,
                    Name = p.Name,
                    Price = p.Price,
                    Description = p.Description,
                    AddedAt = p.AddedAt,
                    ExpiresAt = p.ExpiresAt,
                    Category = p.Category,
                    UserId = p.UserId,
                })
                .ToList();
            return productsDtoList;
        }
        public async Task<List<ProductDTO>> GetUserProductsAsync(int userId)
        {
            var products = await _productRepository.GetProductsByUser(userId);
            return products.Select(p => new ProductDTO
            {
                Id = p.Id,
                Name = p.Name,
                Price = p.Price,
                Description = p.Description,
                AddedAt = p.AddedAt,
                ExpiresAt = p.ExpiresAt,
                Category = p.Category,
                UserId = p.UserId,
            }).ToList();
        }
        public async Task<ProductDetailsDTO?> GetProductByIdAsync(int id)
        {
            var p = await _productRepository.GetProductById(id);
            if (p == null)
                return null;
            return new ProductDetailsDTO
            {
                Id = p.Id,
                Name = p.Name,
                Description = p.Description,
                Price = p.Price,
                AddedAt = p.AddedAt,
                ExpiresAt = p.ExpiresAt,
                Category = p.Category,
                UserId = p.UserId,
                SellerName = p.user?.UserName ?? "",
                SellerEmail = p.user?.Email ?? "",
                SellerSince = p.user?.CreatedAt ?? default,
            };
        }
    }
}
