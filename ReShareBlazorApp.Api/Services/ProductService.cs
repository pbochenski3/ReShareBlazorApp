
using ReShareBlazorApp.Api.Date;
using ReShareBlazorApp.Api.Date.Enums;
using ReShareBlazorApp.Api.Interfaces.Repositories;
using ReShareBlazorApp.Api.Interfaces.Services;
using ReShareBlazorApp.Migrations;
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
            ProductCategory? category = null)
        {
            var listOfProducts = await _productRepository.GetProducts(number, category);
            var productsDtoList = listOfProducts.
                Select(p => new ProductDTO
                {
                    Name = p.Name,
                    Price = p.Price,
                    AddedAt = p.AddedAt,
                    ExpiresAt = p.ExpiresAt,
                    Category = p.Category,
                    UserId = p.UserId,
                })
                .ToList();
            return productsDtoList;
        }
    }
}
