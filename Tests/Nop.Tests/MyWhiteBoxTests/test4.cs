using FluentAssertions;
using Nop.Core.Domain.Catalog;
using Nop.Services.Catalog;
using NUnit.Framework;
using System.Collections.Generic;

namespace Nop.Tests.Nop.Services.Tests.Catalog
{
    /// <summary>
    /// method: `FindProductCategory(IList<ProductCategory>, int, int)`
    ///
    /// - The method contains one statement with a compound condition (&&).
    /// - Condition Coverage:
    ///     - pc.ProductId == productId  → tested true and false
    ///     - pc.CategoryId == categoryId → tested true and false
    ///     - All 4 combinations covered
    ///

    /// </summary>
    public class CategoryServiceTests
    {
        private CategoryService _categoryService;

        [SetUp]
        public void Setup()
        {
            _categoryService = new CategoryService(
                null, null, null, null, null, null, null,
                null, null, null, null, null);
        }
        
        [Test]
        public void FindProductCategory_Should_Return_CorrectMatch()
        {
            var list = new List<ProductCategory>
            {
                new ProductCategory { ProductId = 1, CategoryId = 10 },
                new ProductCategory { ProductId = 2, CategoryId = 20 }
            };

            var result = _categoryService.FindProductCategory(list, 1, 10);

            result.Should().NotBeNull();
            result.ProductId.Should().Be(1);
            result.CategoryId.Should().Be(10);
        }
        [Test]
        public void FindProductCategory_Should_Return_Null_When_CategoryId_NotMatch()
        {
            var list = new List<ProductCategory>
            {
                new ProductCategory { ProductId = 1, CategoryId = 10 }
            };

            var result = _categoryService.FindProductCategory(list, 1, 99);

            result.Should().BeNull();
        }
        [Test]
        public void FindProductCategory_Should_Return_Null_When_ProductId_NotMatch()
        {
            var list = new List<ProductCategory>
            {
                new ProductCategory { ProductId = 1, CategoryId = 10 }
            };

            var result = _categoryService.FindProductCategory(list, 99, 10);

            result.Should().BeNull();
        }

        [Test]
        public void FindProductCategory_Should_Return_Null_When_ListIsEmpty()
        {
            var result = _categoryService.FindProductCategory(new List<ProductCategory>(), 1, 10);

            result.Should().BeNull();
        }
    }
}
