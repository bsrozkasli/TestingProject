using NUnit.Framework;
using Nop.Core;
using System;
using FluentAssertions;

namespace Nop.Tests.Core
{
    [TestFixture]
    public class CommonHelperTests
    {
        [Test]
        public void EnsureMaximumLength_Should_Truncate_String_When_Too_Long()
        {
            string input = "test";
            string result = CommonHelper.EnsureMaximumLength(input, 5);
            result.Should().Be("OpenA");
        }

        [Test]
        public void EnsureMaximumLength_Should_Append_Postfix_When_Truncated()
        {
            string input = "test";
            string result = CommonHelper.EnsureMaximumLength(input, 8, "...");
            result.Should().Be("OpenA...");
        }

        [Test]
        public void EnsureMaximumLength_Should_Return_Same_String_When_Short_Enough()
        {
            string input = "Hello";
            string result = CommonHelper.EnsureMaximumLength(input, 10);
            result.Should().Be("Hello");
        }

        [Test]
        public void IsValidEmail_Should_Return_True_For_Valid_Email()
        {
            CommonHelper.IsValidEmail("user@example.com").Should().BeTrue();
        }

        [Test]
        public void IsValidEmail_Should_Return_False_For_Invalid_Email()
        {
            CommonHelper.IsValidEmail("user@@example..com").Should().BeFalse();
            CommonHelper.IsValidEmail("notanemail").Should().BeFalse();
        }

        [Test]
        public void EnsureNumericOnly_Should_Remove_NonDigits()
        {
            string input = "a1b2c3";
            string result = CommonHelper.EnsureNumericOnly(input);
            result.Should().Be("123");
        }

        [Test]
        public void EnsureNumericOnly_Should_Return_Empty_If_Input_Null()
        {
            string result = CommonHelper.EnsureNumericOnly(null);
            result.Should().Be("");
        }

        [Test]
        public void ArraysEqual_Should_Return_True_For_Identical_Arrays()
        {
            int[] arr1 = { 1, 2, 3 };
            int[] arr2 = { 1, 2, 3 };
            CommonHelper.ArraysEqual(arr1, arr2).Should().BeTrue();
        }

        [Test]
        public void ArraysEqual_Should_Return_False_For_Different_Arrays()
        {
            int[] arr1 = { 1, 2, 3 };
            int[] arr2 = { 1, 2, 4 };
            CommonHelper.ArraysEqual(arr1, arr2).Should().BeFalse();
        }

        [Test]
        public void EnsureNotNull_Should_Return_Empty_For_Null()
        {
            CommonHelper.EnsureNotNull(null).Should().Be("");
        }

        [Test]
        public void EnsureNotNull_Should_Return_Same_If_NotNull()
        {
            CommonHelper.EnsureNotNull("test").Should().Be("test");
        }

        [Test]
        public void AreNullOrEmpty_Should_Return_True_If_Any_Null_Or_Empty()
        {
            CommonHelper.AreNullOrEmpty("a", null, "b").Should().BeTrue();
            CommonHelper.AreNullOrEmpty("a", "", "b").Should().BeTrue();
        }

        [Test]
        public void AreNullOrEmpty_Should_Return_False_If_All_Have_Value()
        {
            CommonHelper.AreNullOrEmpty("a", "b", "c").Should().BeFalse();
        }
    }
}
//C:\Users\basar\RiderProjects\nopCommerce\src\Tests\Nop.Tests\Nop.Tests.csproj.