using System.Collections.Generic;
using System.Diagnostics;
using HVO.Enterprise.Telemetry.Context;
using HVO.Enterprise.Telemetry.Context.Providers;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace HVO.Enterprise.Telemetry.Tests.Context
{
    [TestClass]
    public class UserContextProviderTests
    {
        [TestMethod]
        public void UserContextProvider_AddsUserTags()
        {
            var accessor = new FakeUserContextAccessor(new UserContext
            {
                UserId = "user-1",
                Username = "testuser",
                Roles = new List<string> { "admin" }
            });
            var provider = new UserContextProvider(accessor);
            using (var activity = new Activity("test"))
            {
                var options = new EnrichmentOptions { RedactPii = false };

                provider.EnrichActivity(activity, options);

                Assert.AreEqual("user-1", activity.GetTagItem("user.id"));
                Assert.AreEqual("testuser", activity.GetTagItem("user.name"));
                Assert.AreEqual("admin", activity.GetTagItem("user.roles"));
            }
        }

        [TestMethod]
        public void UserContextProvider_RedactsUserName()
        {
            var accessor = new FakeUserContextAccessor(new UserContext
            {
                Username = "test@example.com"
            });
            var provider = new UserContextProvider(accessor);
            using (var activity = new Activity("test"))
            {
                var options = new EnrichmentOptions { RedactPii = true, RedactionStrategy = PiiRedactionStrategy.Mask };

                provider.EnrichActivity(activity, options);

                Assert.AreEqual("***", activity.GetTagItem("user.name"));
            }
        }

        [TestMethod]
        public void UserContextProvider_RespectsVerboseLevel()
        {
            var accessor = new FakeUserContextAccessor(new UserContext
            {
                Email = "test@example.com",
                TenantId = "tenant-1"
            });
            var provider = new UserContextProvider(accessor);
            using (var activity = new Activity("test"))
            {
                var options = new EnrichmentOptions { MaxLevel = EnrichmentLevel.Standard };

                provider.EnrichActivity(activity, options);

                Assert.IsNull(activity.GetTagItem("user.email"));
                Assert.IsNull(activity.GetTagItem("user.tenant_id"));
            }
        }

        private sealed class FakeUserContextAccessor : IUserContextAccessor
        {
            private readonly UserContext? _context;

            public FakeUserContextAccessor(UserContext? context)
            {
                _context = context;
            }

            public UserContext? GetUserContext()
            {
                return _context;
            }
        }
    }
}
