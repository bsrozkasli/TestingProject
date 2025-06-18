using NUnit.Framework;
using Nop.Services.Customers;
using Nop.Core.Domain.Customers;
using System;
using System.Threading.Tasks;
using FluentAssertions;
using Nop.Core.Domain.Common;

namespace Nop.Tests.Nop.Services.Tests.Customers
{
    [TestFixture]
    public class CustomerServiceIntegrationTests : ServiceTest
    {
        private ICustomerService _customerService;
        private Customer _testCustomer;

        [OneTimeSetUp]
        public void SetUp()
        {
            _customerService = GetService<ICustomerService>();
        }

        [SetUp]
        public async Task TestSetUp()
        {
            _testCustomer = new Customer
            {
                CustomerGuid = Guid.NewGuid(),
                Email = $"test_{Guid.NewGuid()}@test.com",
                Username = $"testuser_{Guid.NewGuid()}",
                Active = true,
                CreatedOnUtc = DateTime.UtcNow,
                LastActivityDateUtc = DateTime.UtcNow
            };
        }

        [TearDown]
        public async Task TestCleanup()
        {
            if (_testCustomer?.Id > 0)
            {
                await _customerService.DeleteCustomerAsync(_testCustomer);
            }
        }

        /// <summary>
        /// Yeni müşteri kaydının veritabanına başarıyla eklendiğini doğrular
        /// </summary>
        [Test]
        public async Task InsertCustomerAsync_ShouldCreateCustomerInDatabase()
        {
            await _customerService.InsertCustomerAsync(_testCustomer);

            _testCustomer.Id.Should().BeGreaterThan(0);
            
            var savedCustomer = await _customerService.GetCustomerByIdAsync(_testCustomer.Id);
            savedCustomer.Should().NotBeNull();
            savedCustomer.Email.Should().Be(_testCustomer.Email);
            savedCustomer.Username.Should().Be(_testCustomer.Username);
        }

        /// <summary>
        /// Mevcut müşteri bilgilerinin veritabanında başarıyla güncellendiğini doğrular
        /// </summary>
        [Test]
        public async Task UpdateCustomerAsync_ShouldUpdateCustomerInDatabase()
        {
            await _customerService.InsertCustomerAsync(_testCustomer);
            var newUsername = $"updated_{Guid.NewGuid()}";
            
            _testCustomer.Username = newUsername;
            await _customerService.UpdateCustomerAsync(_testCustomer);

            var updatedCustomer = await _customerService.GetCustomerByIdAsync(_testCustomer.Id);
            updatedCustomer.Should().NotBeNull();
            updatedCustomer.Username.Should().Be(newUsername);
        }

        /// <summary>
        /// Müşteri şifresinin ekleme ve güncelleme işlemlerinin düzgün çalıştığını doğrular
        /// </summary>
        [Test]
        public async Task InsertAndUpdateCustomerPasswordAsync_ShouldManagePasswordCorrectly()
        {
            await _customerService.InsertCustomerAsync(_testCustomer);
            var password = new CustomerPassword
            {
                CustomerId = _testCustomer.Id,
                Password = "TestPassword123!",
                PasswordFormat = PasswordFormat.Clear,
                CreatedOnUtc = DateTime.UtcNow
            };

            await _customerService.InsertCustomerPasswordAsync(password);

            var savedPassword = await _customerService.GetCurrentPasswordAsync(_testCustomer.Id);
            savedPassword.Should().NotBeNull();
            savedPassword.Password.Should().Be(password.Password);

            savedPassword.Password = "NewPassword456!";
            await _customerService.UpdateCustomerPasswordAsync(savedPassword);

            var updatedPassword = await _customerService.GetCurrentPasswordAsync(_testCustomer.Id);
            updatedPassword.Should().NotBeNull();
            updatedPassword.Password.Should().Be("NewPassword456!");
        }

        /// <summary>
        /// Email adresine göre müşteri arama işleminin doğru sonuç döndürdüğünü doğrular
        /// </summary>
        [Test]
        public async Task GetCustomerByEmailAsync_ShouldReturnCorrectCustomer()
        {
            await _customerService.InsertCustomerAsync(_testCustomer);

            var foundCustomer = await _customerService.GetCustomerByEmailAsync(_testCustomer.Email);

            foundCustomer.Should().NotBeNull();
            foundCustomer.Id.Should().Be(_testCustomer.Id);
            foundCustomer.Email.Should().Be(_testCustomer.Email);
        }
    }
}