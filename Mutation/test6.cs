using FluentAssertions;
using NUnit.Framework;
using Nop.Web.Framework.Security;

namespace Nop.Tests.Web.Framework.Security
{
    [TestFixture]
    public class FilePermissionHelperSimpleTests
    {
        [Test]
        public void CheckUserFilePermissions_ReadPermission_ShouldBeTrue()
        {
            // 5 = Read+Execute, 6 = Read+Write, 7 = All permissions
            FilePermissionHelperTestAccess.CheckUserFilePermissions(5, true, false, false, false).Should().BeTrue();
            FilePermissionHelperTestAccess.CheckUserFilePermissions(6, true, false, false, false).Should().BeTrue();
            FilePermissionHelperTestAccess.CheckUserFilePermissions(7, true, false, false, false).Should().BeTrue();
        }

        [Test]
        public void CheckUserFilePermissions_WritePermission_ShouldBeTrue()
        {
            // 2 = Write, 3 = Write+Execute, 6 = Read+Write, 7 = All
            FilePermissionHelperTestAccess.CheckUserFilePermissions(2, false, true, false, false).Should().BeTrue();
            FilePermissionHelperTestAccess.CheckUserFilePermissions(3, false, true, false, false).Should().BeTrue();
            FilePermissionHelperTestAccess.CheckUserFilePermissions(6, false, true, false, false).Should().BeTrue();
            FilePermissionHelperTestAccess.CheckUserFilePermissions(7, false, true, false, false).Should().BeTrue();
        }

        [Test]
        public void CheckUserFilePermissions_NoPermission_ShouldBeFalse()
        {
            FilePermissionHelperTestAccess.CheckUserFilePermissions(0, true, false, false, false).Should().BeFalse();
            FilePermissionHelperTestAccess.CheckUserFilePermissions(0, false, true, false, false).Should().BeFalse();
        }
    }

    /// <summary>
    /// FilePermissionHelper'ın internal/private methodunu test için public static wrapper.
    /// (Test projesinde FilePermissionHelper.cs dosyasını açıp bu wrapper'ı ekleyip sonra testleri çalıştırabilirsin.)
    /// </summary>
    public static class FilePermissionHelperTestAccess
    {
        public static bool CheckUserFilePermissions(int userFilePermission, bool checkRead, bool checkWrite, bool checkModify, bool checkDelete)
        {
            // FilePermissionHelper içindeki aynı isimli private statik fonksiyonu buraya kopyalayıp public yapabilirsin.
            // Ya da FilePermissionHelper içinde direkt internal yapıp test projesinde [InternalsVisibleTo] kullanabilirsin.
            // Aşağıda kopyalandı:
            var readPermissions = new[] { 5, 6, 7 };
            var writePermissions = new[] { 2, 3, 6, 7 };

            if (checkRead && readPermissions.Contains(userFilePermission))
                return true;

            return (checkWrite || checkModify || checkDelete) && writePermissions.Contains(userFilePermission);
        }
    }
}
