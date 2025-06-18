using FluentAssertions;
using Nop.Core.Domain.Customers;
using Nop.Services.Customers;
using Nop.Services.Security;
using NUnit.Framework;

namespace Nop.Tests.Nop.Services.Tests.Customers;

[TestFixture]
public class CustomerRegistrationServiceTest: ServiceTest
{
    private ICustomerService _customerService;
    private IEncryptionService _encryptionService;
    private ICustomerRegistrationService _customerRegistrationService;

    [OneTimeSetUp]
    public void SetUp()
    {
        _customerService = GetService<ICustomerService>();
        _encryptionService = GetService<IEncryptionService>();
        _customerRegistrationService = GetService<ICustomerRegistrationService>();
    }

    private async Task<Customer> CreateCustomerAsync(PasswordFormat passwordFormat = PasswordFormat.Clear, bool isRegistered = true, string email = "test@test.com", string username = "testuser", bool isActive = true, string password = "password")
    {
        var customer = new Customer
        {
            Username = username,
            Email = email,
            Active = isActive,
            CreatedOnUtc = DateTime.UtcNow
        };

        await _customerService.InsertCustomerAsync(customer);

        var pwd = password;
        if (passwordFormat == PasswordFormat.Encrypted)
            pwd = _encryptionService.EncryptText(password);
        else if (passwordFormat == PasswordFormat.Hashed)
            pwd = _encryptionService.CreatePasswordHash(password, "somesalt", "SHA512");

        await _customerService.InsertCustomerPasswordAsync(new CustomerPassword
        {
            CustomerId = customer.Id,
            PasswordFormat = passwordFormat,
            Password = pwd,
            PasswordSalt = passwordFormat == PasswordFormat.Hashed ? "somesalt" : null,
            CreatedOnUtc = DateTime.UtcNow
        });

        if (isRegistered)
        {
            var registeredRole = await _customerService
                .GetCustomerRoleBySystemNameAsync(NopCustomerDefaults.RegisteredRoleName);
            await _customerService.AddCustomerRoleMappingAsync(new CustomerCustomerRoleMapping
            {
                CustomerId = customer.Id,
                CustomerRoleId = registeredRole.Id
            });
        }

        return customer;
    }

    private async Task DeleteCustomerAsync(Customer customer)
    {
        customer.Username = customer.Email = string.Empty;
        customer.Active = false;
        await _customerService.UpdateCustomerAsync(customer);
        await _customerService.DeleteCustomerAsync(customer);
    }

    [Test]
    public async Task EnsureOnlyRegisteredCustomersCanLogin()
    {
        var result = await _customerRegistrationService.ValidateCustomerAsync(NopTestsDefaults.AdminEmail, NopTestsDefaults.AdminPassword);
        result.Should().Be(CustomerLoginResults.Successful);

        var customer = await CreateCustomerAsync(PasswordFormat.Clear, false);

        result = await _customerRegistrationService.ValidateCustomerAsync("test@test.com", "password");
        await DeleteCustomerAsync(customer);

        result.Should().Be(CustomerLoginResults.NotRegistered);
    }

    [Test]
    public async Task CanValidateHashedPassword()
    {
        var result = await _customerRegistrationService.ValidateCustomerAsync(NopTestsDefaults.AdminEmail, NopTestsDefaults.AdminPassword);
        result.Should().Be(CustomerLoginResults.Successful);
    }

    [Test]
    public async Task CanValidateClearPassword()
    {
        var customer = await CreateCustomerAsync(PasswordFormat.Clear);

        var result = await _customerRegistrationService.ValidateCustomerAsync("test@test.com", "password");
        await DeleteCustomerAsync(customer);

        result.Should().Be(CustomerLoginResults.Successful);
    }

    [Test]
    public async Task CanValidateEncryptedPassword()
    {
        var customer = await CreateCustomerAsync(PasswordFormat.Encrypted);

        var result = await _customerRegistrationService.ValidateCustomerAsync("test@test.com", "password");
        await DeleteCustomerAsync(customer);

        result.Should().Be(CustomerLoginResults.Successful);
    }

    [Test]
    public async Task CanChangePassword()
    {
        var customer = await CreateCustomerAsync(PasswordFormat.Encrypted);

        var request = new ChangePasswordRequest("test@test.com", true, PasswordFormat.Clear, "password", "password");
        var unSuccess = await _customerRegistrationService.ChangePasswordAsync(request);

        request = new ChangePasswordRequest("test@test.com", true, PasswordFormat.Hashed, "newpassword", "password");
        var success = await _customerRegistrationService.ChangePasswordAsync(request);

        unSuccess.Success.Should().BeFalse();
        success.Success.Should().BeTrue();

        await DeleteCustomerAsync(customer);
    }

 

    [Test]
    public async Task ValidateCustomer_WithNullEmail_ShouldReturnCustomerNotExist()
    {
        var result = await _customerRegistrationService.ValidateCustomerAsync(null, "password");
        result.Should().Be(CustomerLoginResults.CustomerNotExist);
    }

    [Test]
    public async Task ValidateCustomer_WithEmptyEmail_ShouldReturnCustomerNotExist()
    {
        var result = await _customerRegistrationService.ValidateCustomerAsync("", "password");
        result.Should().Be(CustomerLoginResults.CustomerNotExist);
    }

    [Test]
    public async Task ValidateCustomer_WithWhitespaceEmail_ShouldReturnCustomerNotExist()
    {
        var result = await _customerRegistrationService.ValidateCustomerAsync("   ", "password");
        result.Should().Be(CustomerLoginResults.CustomerNotExist);
    }

    [Test]
    public async Task ValidateCustomer_WithNullPassword_ShouldReturnWrongPassword()
    {
        var customer = await CreateCustomerAsync();
        var result = await _customerRegistrationService.ValidateCustomerAsync("test@test.com", null);
        await DeleteCustomerAsync(customer);
        result.Should().Be(CustomerLoginResults.WrongPassword);
    }

    [Test]
    public async Task ValidateCustomer_WithEmptyPassword_ShouldReturnWrongPassword()
    {
        var customer = await CreateCustomerAsync();
        var result = await _customerRegistrationService.ValidateCustomerAsync("test@test.com", "");
        await DeleteCustomerAsync(customer);
        result.Should().Be(CustomerLoginResults.WrongPassword);
    }

    [Test]
    public async Task ValidateCustomer_WithDeletedCustomer_ShouldReturnDeleted()
    {
        var customer = await CreateCustomerAsync();
        customer.Deleted = true;
        await _customerService.UpdateCustomerAsync(customer);
        var result = await _customerRegistrationService.ValidateCustomerAsync("test@test.com", "password");
        await DeleteCustomerAsync(customer);
        result.Should().Be(CustomerLoginResults.Deleted);
    }

    [Test]
    public async Task ValidateCustomer_WithNonExistentEmail_ShouldReturnCustomerNotExist()
    {
        var result = await _customerRegistrationService.ValidateCustomerAsync("nonexistent@email.com", "password");
        result.Should().Be(CustomerLoginResults.CustomerNotExist);
    }

    [Test]
    public async Task ValidateCustomer_WithWrongPassword_ShouldReturnWrongPassword()
    {
        var customer = await CreateCustomerAsync(password: "CorrectPassword!");
        var result = await _customerRegistrationService.ValidateCustomerAsync("test@test.com", "wrongPassword");
        await DeleteCustomerAsync(customer);
        result.Should().Be(CustomerLoginResults.WrongPassword);
    }

    [Test]
    public async Task ValidateCustomer_WithCaseSensitivePassword_ShouldReturnWrongPassword()
    {
        var customer = await CreateCustomerAsync(password: "AbcDEF123!");
        var result = await _customerRegistrationService.ValidateCustomerAsync("test@test.com", "abcdef123!");
        await DeleteCustomerAsync(customer);
        result.Should().Be(CustomerLoginResults.WrongPassword);
    }

    [Test]
    public async Task ChangePassword_WithWrongOldPassword_ShouldFail()
    {
        var customer = await CreateCustomerAsync();
        var request = new ChangePasswordRequest("test@test.com", true, PasswordFormat.Hashed, "newpassword", "wrongOldPassword");
        var result = await _customerRegistrationService.ChangePasswordAsync(request);
        await DeleteCustomerAsync(customer);

        result.Success.Should().BeFalse();
    }

    [Test]
    public async Task ChangePassword_WithValidRequest_ShouldSucceedAndInvalidateOldPassword()
    {
        var customer = await CreateCustomerAsync(password: "password");
        var newPassword = "newStrongPassword1!";

        var request = new ChangePasswordRequest("test@test.com", true, PasswordFormat.Hashed, newPassword, "password");
        var changeResult = await _customerRegistrationService.ChangePasswordAsync(request);

        var oldPasswordValidation = await _customerRegistrationService.ValidateCustomerAsync("test@test.com", "password");
        var newPasswordValidation = await _customerRegistrationService.ValidateCustomerAsync("test@test.com", newPassword);

        await DeleteCustomerAsync(customer);

        changeResult.Success.Should().BeTrue();
        oldPasswordValidation.Should().Be(CustomerLoginResults.WrongPassword);
        newPasswordValidation.Should().Be(CustomerLoginResults.Successful);
    }

    [Test]
    public async Task ValidateCustomer_ConcurrentValidations_ShouldHandleCorrectly()
    {
        var customer = await CreateCustomerAsync();
        
        var tasks = Enumerable.Range(0, 10).Select(async _ =>
            await _customerRegistrationService.ValidateCustomerAsync("test@test.com", "password"));
        
        var results = await Task.WhenAll(tasks);
        await DeleteCustomerAsync(customer);
        
        results.Should().AllSatisfy(result => result.Should().Be(CustomerLoginResults.Successful));
    }

    [Test]
    public async Task ValidateCustomer_WithDifferentPasswordFormats_ShouldWorkCorrectly()
    {
        var testCases = new[]
        {
            PasswordFormat.Clear,
            PasswordFormat.Encrypted,
            PasswordFormat.Hashed
        };

        foreach (var format in testCases)
        {
            var customer = await CreateCustomerAsync(passwordFormat: format);
            var result = await _customerRegistrationService.ValidateCustomerAsync("test@test.com", "password");
            await DeleteCustomerAsync(customer);
            
            result.Should().Be(CustomerLoginResults.Successful);
        }
    }

    [Test]
    public async Task ChangePassword_WithDifferentPasswordFormats_ShouldWorkCorrectly()
    {
        var testCases = new[]
        {
            PasswordFormat.Clear,
            PasswordFormat.Encrypted,
            PasswordFormat.Hashed
        };

        foreach (var format in testCases)
        {
            var customer = await CreateCustomerAsync(passwordFormat: format);
            var request = new ChangePasswordRequest("test@test.com", true, PasswordFormat.Hashed, 
                $"newPassword{format}!", "password");
            
            var result = await _customerRegistrationService.ChangePasswordAsync(request);
            await DeleteCustomerAsync(customer);
            
            result.Success.Should().BeTrue();
        }
    }

    [Test]
    public async Task ValidateCustomer_WithVeryLongPassword_ShouldHandleGracefully()
    {
        var customer = await CreateCustomerAsync();
        var veryLongPassword = new string('a', 1000);
        var result = await _customerRegistrationService.ValidateCustomerAsync("test@test.com", veryLongPassword);
        await DeleteCustomerAsync(customer);
        result.Should().Be(CustomerLoginResults.WrongPassword);
    }

    [Test]
    public async Task ChangePassword_WithComplexPasswordPatterns_ShouldSucceed()
    {
        var complexPasswords = new[]
        {
            "P@$$w0rd!2024",
            "Mix3d-Ch4r@ct3r$"
        };

        foreach (var complexPassword in complexPasswords)
        {
            var customer = await CreateCustomerAsync();
            var request = new ChangePasswordRequest("test@test.com", true, PasswordFormat.Hashed, 
                complexPassword, "password");
            
            var result = await _customerRegistrationService.ChangePasswordAsync(request);
            await DeleteCustomerAsync(customer);
            
            result.Success.Should().BeTrue();
        }
    }

    [Test]
    public async Task ValidateCustomer_WithMalformedEmails_ShouldReturnCustomerNotExist()
    {
        var malformedEmails = new[]
        {
            "@test.com",
            "test@",
            "test test@test.com"
        };

        foreach (var email in malformedEmails)
        {
            var result = await _customerRegistrationService.ValidateCustomerAsync(email, "password");
            result.Should().Be(CustomerLoginResults.CustomerNotExist);
        }
    }

    [Test]
    public async Task ValidateCustomer_RealWorldScenario_LoginAfterRegistration()
    {
        var email = $"newuser_{Guid.NewGuid()}@test.com";
        var password = "MySecurePassword123!";
        
        var customer = await CreateCustomerAsync(email: email, password: password);
        
        var loginResult = await _customerRegistrationService.ValidateCustomerAsync(email, password);
        await DeleteCustomerAsync(customer);
        
        loginResult.Should().Be(CustomerLoginResults.Successful);
    }

    [Test]
    public async Task ChangePassword_RealWorldScenario_PasswordRecovery()
    {
        var customer = await CreateCustomerAsync();
        
        var oldLoginResult = await _customerRegistrationService.ValidateCustomerAsync("test@test.com", "password");
        
        var request = new ChangePasswordRequest("test@test.com", false, PasswordFormat.Hashed, 
            "NewRecoveredPassword123!", null);
        var changeResult = await _customerRegistrationService.ChangePasswordAsync(request);
        
        var newLoginResult = await _customerRegistrationService.ValidateCustomerAsync("test@test.com", "NewRecoveredPassword123!");
        await DeleteCustomerAsync(customer);
        
        oldLoginResult.Should().Be(CustomerLoginResults.Successful);
        changeResult.Success.Should().BeTrue();
        newLoginResult.Should().Be(CustomerLoginResults.Successful);
    }
}