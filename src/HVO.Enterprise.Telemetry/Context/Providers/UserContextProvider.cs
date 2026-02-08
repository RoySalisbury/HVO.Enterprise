using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace HVO.Enterprise.Telemetry.Context.Providers
{
    /// <summary>
    /// Enriches telemetry with user authentication context.
    /// </summary>
    public sealed class UserContextProvider : IContextProvider
    {
        private readonly IUserContextAccessor _userAccessor;
        private readonly PiiRedactor _piiRedactor;

        /// <summary>
        /// Initializes a new instance of the <see cref="UserContextProvider"/> class.
        /// </summary>
        /// <param name="userAccessor">Optional user context accessor.</param>
        public UserContextProvider(IUserContextAccessor? userAccessor = null)
        {
            _userAccessor = userAccessor ?? new DefaultUserContextAccessor();
            _piiRedactor = new PiiRedactor();
        }

        /// <inheritdoc />
        public string Name => "User";

        /// <inheritdoc />
        public EnrichmentLevel Level => EnrichmentLevel.Standard;

        /// <inheritdoc />
        public void EnrichActivity(Activity activity, EnrichmentOptions options)
        {
            if (activity == null)
                throw new ArgumentNullException(nameof(activity));
            if (options == null)
                throw new ArgumentNullException(nameof(options));

            var userContext = _userAccessor.GetUserContext();
            if (userContext == null)
                return;

            AddSafeTag(activity, "user.id", userContext.UserId, options);
            AddSafeTag(activity, "user.name", userContext.Username, options);

            if (userContext.Roles != null && userContext.Roles.Count > 0)
                activity.SetTag("user.roles", string.Join(",", userContext.Roles));

            if (options.MaxLevel >= EnrichmentLevel.Verbose)
            {
                AddSafeTag(activity, "user.email", userContext.Email, options);
                AddSafeTag(activity, "user.tenant_id", userContext.TenantId, options);
            }
        }

        /// <inheritdoc />
        public void EnrichProperties(IDictionary<string, object> properties, EnrichmentOptions options)
        {
            if (properties == null)
                throw new ArgumentNullException(nameof(properties));
            if (options == null)
                throw new ArgumentNullException(nameof(options));

            var userContext = _userAccessor.GetUserContext();
            if (userContext == null)
                return;

            AddSafeProperty(properties, "user.id", userContext.UserId, options);
            AddSafeProperty(properties, "user.name", userContext.Username, options);

            if (userContext.Roles != null && userContext.Roles.Count > 0)
                properties["user.roles"] = userContext.Roles;

            if (options.MaxLevel >= EnrichmentLevel.Verbose)
            {
                AddSafeProperty(properties, "user.email", userContext.Email, options);
                AddSafeProperty(properties, "user.tenant_id", userContext.TenantId, options);
            }
        }

        private void AddSafeTag(Activity activity, string key, string? value, EnrichmentOptions options)
        {
            var safeValue = value;
            if (safeValue == null || safeValue.Length == 0)
                return;

            if (_piiRedactor.TryRedact(key, safeValue, options, out var redacted) && redacted == null)
                return;

            activity.SetTag(key, redacted ?? safeValue);
        }

        private void AddSafeProperty(IDictionary<string, object> properties, string key, string? value, EnrichmentOptions options)
        {
            var safeValue = value;
            if (safeValue == null || safeValue.Length == 0)
                return;

            if (_piiRedactor.TryRedact(key, safeValue, options, out var redacted) && redacted == null)
                return;

            properties[key] = redacted ?? safeValue;
        }
    }
}
