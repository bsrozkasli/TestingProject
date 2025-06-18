using FluentAssertions;
using Nop.Core.Domain.Catalog;
using Nop.Services.Catalog;
using NUnit.Framework;

namespace Nop.Tests.Nop.Services.Tests.Catalog;

[TestFixture]
public class ProductServiceWhiteBoxTests : ServiceTest
{
    #region Fields

    private IProductService _productService;

    #endregion

    #region SetUp

    [OneTimeSetUp]
    public async Task SetUp()
    {
        _productService = GetService<IProductService>();
    }

    #endregion

    #region Edge Cases and Error Handling Tests

    /// <summary>
    /// Gerekli ürün ID'lerinin null, boş veya whitespace değerlerle çağrıldığında boş dizi döndürdüğünü doğrular
    /// </summary>
    [Test]
    public void ParseRequiredProductIds_WithNullOrEmptyString_ShouldReturnEmptyArray()
    {
        var productWithNull = new Product { RequiredProductIds = null };
        var idsNull = _productService.ParseRequiredProductIds(productWithNull);
        idsNull.Should().BeEmpty();

        var productWithEmpty = new Product { RequiredProductIds = "" };
        var idsEmpty = _productService.ParseRequiredProductIds(productWithEmpty);
        idsEmpty.Should().BeEmpty();

        var productWithWhitespace = new Product { RequiredProductIds = "   " };
        var idsWhitespace = _productService.ParseRequiredProductIds(productWithWhitespace);
        idsWhitespace.Should().BeEmpty();
    }

    /// <summary>
    /// Sadece geçersiz değerler içeren gerekli ürün ID'leri için boş dizi döndürdüğünü doğrular
    /// </summary>
    [Test]
    public void ParseRequiredProductIds_WithOnlyInvalidValues_ShouldReturnEmptyArray()
    {
        var product = new Product { RequiredProductIds = "a,b,c,," };
        var ids = _productService.ParseRequiredProductIds(product);
        ids.Should().BeEmpty();
    }

    /// <summary>
    /// İzin verilen miktarların null veya boş string değerlerle çağrıldığında boş dizi döndürdüğünü doğrular
    /// </summary>
    [Test]
    public void ParseAllowedQuantities_WithNullOrEmptyString_ShouldReturnEmptyArray()
    {
        var productWithNull = new Product { AllowedQuantities = null };
        var quantitiesNull = _productService.ParseAllowedQuantities(productWithNull);
        quantitiesNull.Should().BeEmpty();

        var productWithEmpty = new Product { AllowedQuantities = "" };
        var quantitiesEmpty = _productService.ParseAllowedQuantities(productWithEmpty);
        quantitiesEmpty.Should().BeEmpty();
    }

    #endregion

    #region Business Logic Tests

    /// <summary>
    /// Başlangıç ve bitiş tarihleri olan ürünün müsaitlik durumunun doğru hesaplandığını doğrular
    /// </summary>
    [Test]
    public void ProductIsAvailable_WithBothStartAndEndDates_ShouldWorkCorrectly()
    {
        var product = new Product
        {
            AvailableStartDateTimeUtc = new DateTime(2024, 1, 1),
            AvailableEndDateTimeUtc = new DateTime(2024, 12, 31)
        };

        _productService.ProductIsAvailable(product, new DateTime(2024, 6, 15)).Should().BeTrue();
        _productService.ProductIsAvailable(product, new DateTime(2023, 12, 31)).Should().BeFalse();
        _productService.ProductIsAvailable(product, new DateTime(2025, 1, 1)).Should().BeFalse();
        _productService.ProductIsAvailable(product, new DateTime(2024, 1, 1)).Should().BeTrue();
        _productService.ProductIsAvailable(product, new DateTime(2024, 12, 31)).Should().BeTrue();
    }

    /// <summary>
    /// Stok yönetimi yapılmayan ürünler için toplam stok miktarının sıfır döndüğünü doğrular
    /// </summary>
    [Test]
    public async Task GetTotalStockQuantity_WithDontManageStockMethod_ShouldReturnZero()
    {
        var product = new Product 
        { 
            StockQuantity = 10, 
            ManageInventoryMethod = ManageInventoryMethod.DontManageStock 
        };
        
        var result = await _productService.GetTotalStockQuantityAsync(product);
        result.Should().Be(0);
    }

    /// <summary>
    /// Özellikler bazında stok yönetimi yapılan ürünler için toplam stok miktarının sıfır döndüğünü doğrular
    /// </summary>
    [Test]
    public async Task GetTotalStockQuantity_WithManageStockByAttributes_ShouldReturnZero()
    {
        var product = new Product 
        { 
            StockQuantity = 10, 
            ManageInventoryMethod = ManageInventoryMethod.ManageStockByAttributes 
        };
        
        var result = await _productService.GetTotalStockQuantityAsync(product);
        result.Should().Be(0);
    }

    #endregion

    #region Rental Period Edge Cases

    /// <summary>
    /// Bitiş tarihi başlangıç tarihinden önce olan kiralama periyodunun doğru işlendiğini doğrular
    /// </summary>
    [Test]
    public void GetRentalPeriods_WithEndDateBeforeStartDate_ShouldHandleCorrectly()
    {
        var product = new Product
        {
            IsRental = true,
            RentalPricePeriod = RentalPricePeriod.Days,
            RentalPriceLength = 1
        };

        var result = _productService.GetRentalPeriods(product, 
            new DateTime(2024, 1, 10), new DateTime(2024, 1, 5));
        
        result.Should().Be(1);
    }

    /// <summary>
    /// Artık yıl Şubat ayını içeren kiralama periyodu hesaplamasının doğru çalıştığını doğrular
    /// </summary>
    [Test]
    public void GetRentalPeriods_WithLeapYear_ShouldCalculateCorrectly()
    {
        var product = new Product
        {
            IsRental = true,
            RentalPricePeriod = RentalPricePeriod.Months,
            RentalPriceLength = 1
        };

        var result = _productService.GetRentalPeriods(product, 
            new DateTime(2024, 2, 1), new DateTime(2024, 2, 29));
        
        result.Should().Be(1);
        
        var result2 = _productService.GetRentalPeriods(product, 
            new DateTime(2024, 2, 1), new DateTime(2024, 3, 1));
        
        result2.Should().Be(1);
    }

    #endregion

    #region Performance and Boundary Tests

    /// <summary>
    /// Büyük sayılarla gerekli ürün ID'lerinin doğru ayrıştırıldığını doğrular
    /// </summary>
    [Test]
    public void ParseRequiredProductIds_WithLargeNumbers_ShouldHandleCorrectly()
    {
        var product = new Product { RequiredProductIds = "2147483647,1,2147483646" };
        var ids = _productService.ParseRequiredProductIds(product);
        
        ids.Should().HaveCount(3);
        ids.Should().Contain(2147483647);
        ids.Should().Contain(2147483646);
    }

    /// <summary>
    /// Çok sayıda miktar değeri içeren büyük string'in doğru işlendiğini doğrular
    /// </summary>
    [Test]
    public void ParseAllowedQuantities_WithLargeString_ShouldHandleCorrectly()
    {
        var quantities = string.Join(",", Enumerable.Range(1, 1000));
        var product = new Product { AllowedQuantities = quantities };
        
        var result = _productService.ParseAllowedQuantities(product);
        result.Should().HaveCount(1000);
        result.First().Should().Be(1);
        result.Last().Should().Be(1000);
    }

    #endregion

    #region Date Precision Tests

    /// <summary>
    /// Milisaniye hassasiyetinde tarih kontrolünün doğru çalıştığını doğrular
    /// </summary>
    [Test]
    public void ProductIsAvailable_WithMillisecondPrecision_ShouldWorkCorrectly()
    {
        var product = new Product
        {
            AvailableStartDateTimeUtc = new DateTime(2024, 1, 1, 12, 0, 0, 500),
            AvailableEndDateTimeUtc = new DateTime(2024, 1, 1, 12, 0, 0, 600)
        };

        _productService.ProductIsAvailable(product, 
            new DateTime(2024, 1, 1, 12, 0, 0, 550)).Should().BeTrue();
        
        _productService.ProductIsAvailable(product, 
            new DateTime(2024, 1, 1, 12, 0, 0, 400)).Should().BeFalse();
        
        _productService.ProductIsAvailable(product, 
            new DateTime(2024, 1, 1, 12, 0, 0, 700)).Should().BeFalse();
    }

    #endregion

    #region Quantity Validation Tests

    /// <summary>
    /// Çok büyük sayılar içeren izin verilen miktarların doğru işlendiğini doğrular
    /// </summary>
    [Test]
    public void ParseAllowedQuantities_WithVeryLargeNumbers_ShouldHandleCorrectly()
    {
        var product = new Product { AllowedQuantities = "1,999999999,2" };
        var quantities = _productService.ParseAllowedQuantities(product);
        
        quantities.Should().Contain(999999999);
        quantities.Should().HaveCount(3);
    }

    #endregion

    #region Additional Rental Period Tests

    /// <summary>
    /// Saat hassasiyetindeki kiralama periyodu hesaplamasının doğru yuvarlandığını doğrular
    /// </summary>
    [Test]
    public void GetRentalPeriods_WithHourPrecision_ShouldRoundCorrectly()
    {
        var product = new Product
        {
            IsRental = true,
            RentalPricePeriod = RentalPricePeriod.Days,
            RentalPriceLength = 1
        };

        var result = _productService.GetRentalPeriods(product, 
            new DateTime(2024, 1, 1, 10, 0, 0), 
            new DateTime(2024, 1, 1, 14, 0, 0));
        
        result.Should().Be(1);
    }

    /// <summary>
    /// Maksimum DateTime değerleriyle kiralama periyodu hesaplamasının hata fırlatmadığını doğrular
    /// </summary>
    [Test]
    public void GetRentalPeriods_WithMaxDateTime_ShouldNotThrowException()
    {
        var product = new Product
        {
            IsRental = true,
            RentalPricePeriod = RentalPricePeriod.Days,
            RentalPriceLength = 1
        };

        var action = () => _productService.GetRentalPeriods(product, 
            DateTime.MinValue, DateTime.MaxValue);
        
        action.Should().NotThrow();
    }

    #endregion
}