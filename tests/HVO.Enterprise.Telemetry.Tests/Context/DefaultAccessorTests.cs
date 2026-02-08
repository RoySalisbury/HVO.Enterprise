using System;
using System.Security.Claims;
using System.Threading;
using HVO.Enterprise.Telemetry.Context.Providers;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace HVO.Enterprise.Telemetry.Tests.Context
{
    [TestClass]
    public class DefaultAccessorTests
    {
        [TestMethod]
        public void DefaultHttpRequestAccessor_UsesAsyncLocalStore()
        {
            var accessor = new DefaultHttpRequestAccessor();
            var request = new HttpRequestInfo { Method = "GET", Path = "/" };

            HttpRequestContextStore.Current = request;
            try
            {
                var current = accessor.GetCurrentRequest();

                Assert.AreSame(request, current);
            }
            finally
            {
                HttpRequestContextStore.Current = null;
            }
        }

        [TestMethod]
        public void DefaultHttpRequestAccessor_ReturnsNullWithoutContext()
        {
            var accessor = new DefaultHttpRequestAccessor();

            HttpRequestContextStore.Current = null;
            var current = accessor.GetCurrentRequest();

            Assert.IsNull(current);
        }

        [TestMethod]
        public void DefaultWcfRequestAccessor_UsesAsyncLocalStore()
        {
            var accessor = new DefaultWcfRequestAccessor();
            var request = new WcfRequestInfo { Action = "action" };

            WcfRequestContextStore.Current = request;
            try
            {
                var current = accessor.GetCurrentRequest();

                Assert.AreSame(request, current);
            }
            finally
            {
                WcfRequestContextStore.Current = null;
            }
        }

        [TestMethod]
        public void DefaultWcfRequestAccessor_ReturnsNullWithoutContext()
        {
            var accessor = new DefaultWcfRequestAccessor();

            WcfRequestContextStore.Current = null;
            var current = accessor.GetCurrentRequest();

            Assert.IsNull(current);
        }

        [TestMethod]
        public void DefaultUserContextAccessor_UsesClaimsPrincipal()
        {
            var accessor = new DefaultUserContextAccessor();
            var original = Thread.CurrentPrincipal;

            try
            {
                var identity = new ClaimsIdentity("test");
                identity.AddClaim(new Claim(ClaimTypes.NameIdentifier, "user-1"));
                identity.AddClaim(new Claim(ClaimTypes.Email, "user@example.com"));
                identity.AddClaim(new Claim(ClaimTypes.Role, "admin"));
                identity.AddClaim(new Claim("tenant_id", "tenant-1"));
                identity.AddClaim(new Claim(ClaimTypes.Name, "testuser"));

                Thread.CurrentPrincipal = new ClaimsPrincipal(identity);

                var context = accessor.GetUserContext();

                Assert.IsNotNull(context);
                Assert.AreEqual("user-1", context!.UserId);
                Assert.AreEqual("user@example.com", context.Email);
                Assert.AreEqual("tenant-1", context.TenantId);
                Assert.AreEqual("testuser", context.Username);
                CollectionAssert.Contains(context.Roles!, "admin");
            }
            finally
            {
                Thread.CurrentPrincipal = original;
            }
        }

        [TestMethod]
        public void DefaultUserContextAccessor_ReturnsNullWhenUnauthenticated()
        {
            var accessor = new DefaultUserContextAccessor();
            var original = Thread.CurrentPrincipal;

            try
            {
                if (!System.Runtime.InteropServices.RuntimeInformation.IsOSPlatform(System.Runtime.InteropServices.OSPlatform.Windows))
                {
                    Assert.Inconclusive("WindowsIdentity is not supported on this platform.");
                }

                Thread.CurrentPrincipal = new ClaimsPrincipal(new ClaimsIdentity());

                var context = accessor.GetUserContext();

                Assert.IsNull(context);
            }
            finally
            {
                Thread.CurrentPrincipal = original;
            }
        }
    }
}
