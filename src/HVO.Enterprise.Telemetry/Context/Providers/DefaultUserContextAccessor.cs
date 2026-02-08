using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading;

namespace HVO.Enterprise.Telemetry.Context.Providers
{
    /// <summary>
    /// Default user context accessor using the current principal.
    /// </summary>
    internal sealed class DefaultUserContextAccessor : IUserContextAccessor
    {
        /// <inheritdoc />
        public UserContext? GetUserContext()
        {
            var principal = Thread.CurrentPrincipal as ClaimsPrincipal;
            if (principal != null && principal.Identity != null && principal.Identity.IsAuthenticated)
                return ExtractFromClaimsPrincipal(principal);

            var windowsIdentity = GetWindowsIdentity();
            if (windowsIdentity != null && windowsIdentity.IsAuthenticated)
                return ExtractFromWindowsIdentity(windowsIdentity);

            return null;
        }

        private static UserContext ExtractFromClaimsPrincipal(ClaimsPrincipal principal)
        {
            var context = new UserContext
            {
                Username = principal.Identity != null ? principal.Identity.Name : null
            };

            context.UserId = principal.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            context.Email = principal.FindFirst(ClaimTypes.Email)?.Value;
            context.TenantId = principal.FindFirst("tenant_id")?.Value;

            var roles = principal.FindAll(ClaimTypes.Role).Select(role => role.Value).ToList();
            if (roles.Count > 0)
                context.Roles = roles;

            return context;
        }

        private static UserContext ExtractFromWindowsIdentity(WindowsIdentityAdapter identity)
        {
            return new UserContext
            {
                UserId = identity.UserId,
                Username = identity.Name
            };
        }

        private static WindowsIdentityAdapter? GetWindowsIdentity()
        {
            var identityType = Type.GetType("System.Security.Principal.WindowsIdentity")
                ?? Type.GetType("System.Security.Principal.WindowsIdentity, System.Security.Principal.Windows")
                ?? Type.GetType("System.Security.Principal.WindowsIdentity, mscorlib");

            if (identityType == null)
                return null;

            var getCurrentMethod = identityType.GetMethod("GetCurrent", Type.EmptyTypes);
            if (getCurrentMethod == null)
                return null;

            var identity = getCurrentMethod.Invoke(null, null);
            if (identity == null)
                return null;

            return new WindowsIdentityAdapter(identity);
        }

        private sealed class WindowsIdentityAdapter
        {
            private readonly object _identity;
            private readonly Type _identityType;

            public WindowsIdentityAdapter(object identity)
            {
                _identity = identity;
                _identityType = identity.GetType();
            }

            public bool IsAuthenticated => GetBoolean("IsAuthenticated");

            public string? Name => GetString("Name");

            public string? UserId
            {
                get
                {
                    var user = GetPropertyValue("User");
                    if (user == null)
                        return null;

                    var userType = user.GetType();
                    var valueProperty = userType.GetProperty("Value");
                    return valueProperty != null ? valueProperty.GetValue(user, null) as string : null;
                }
            }

            private bool GetBoolean(string propertyName)
            {
                var value = GetPropertyValue(propertyName);
                return value is bool flag && flag;
            }

            private string? GetString(string propertyName)
            {
                return GetPropertyValue(propertyName) as string;
            }

            private object? GetPropertyValue(string propertyName)
            {
                var property = _identityType.GetProperty(propertyName);
                return property != null ? property.GetValue(_identity, null) : null;
            }
        }
    }
}
