using System;
using Microsoft.Extensions.DependencyInjection;
using PrimeERP.Application.DTOs.Security;
using PrimeERP.Application.PageServices.Security;
using PrimeERP.Platform.Permissions;
using PrimeERP.Platform.Security;
using Xunit;

namespace PrimeERP.Tests.Services
{
    public class UserPasswordTests : IDisposable
    {
        private readonly TestDatabaseFixture _db = new();

        public UserPasswordTests()
        {
            var admin = _db.Services.GetRequiredService<IPermissionStore>().FindByUsername("admin");
            AppSession.SignIn(admin.Id, admin.Username, admin.DisplayName, admin.RoleId, "", Array.Empty<string>());
        }

        public void Dispose()
        {
            AppSession.SignOut();
            _db.Dispose();
        }

        private IUserService Users => _db.Services.GetRequiredService<IUserService>();

        [Fact]
        public void AWrongCurrentPassword_IsRefused() =>
            Assert.False(Users.ChangeOwnPassword(new ChangePasswordDto { CurrentPassword = "x", NewPassword = "newpassword", ConfirmPassword = "newpassword" }).IsSuccess);

        [Fact]
        public void AMismatchedConfirmation_IsRefused() =>
            Assert.False(Users.ChangeOwnPassword(new ChangePasswordDto { CurrentPassword = "admin", NewPassword = "newpassword", ConfirmPassword = "other" }).IsSuccess);

        [Fact]
        public void TheOwnPassword_Changes()
        {
            var changed = Users.ChangeOwnPassword(new ChangePasswordDto { CurrentPassword = "admin", NewPassword = "newpassword", ConfirmPassword = "newpassword" });

            Assert.True(changed.IsSuccess, changed.ErrorMessage);
            var admin = _db.Services.GetRequiredService<IPermissionStore>().FindByUsername("admin");
            Assert.True(PasswordHasher.Verify("newpassword", admin.PasswordHash, admin.Salt));
        }
    }
}
