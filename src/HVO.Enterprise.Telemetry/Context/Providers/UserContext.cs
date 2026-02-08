using System.Collections.Generic;

namespace HVO.Enterprise.Telemetry.Context.Providers
{
    /// <summary>
    /// Represents user authentication context.
    /// </summary>
    public sealed class UserContext
    {
        /// <summary>
        /// Gets or sets the user identifier.
        /// </summary>
        public string? UserId { get; set; }

        /// <summary>
        /// Gets or sets the username.
        /// </summary>
        public string? Username { get; set; }

        /// <summary>
        /// Gets or sets the email address.
        /// </summary>
        public string? Email { get; set; }

        /// <summary>
        /// Gets or sets the tenant identifier.
        /// </summary>
        public string? TenantId { get; set; }

        /// <summary>
        /// Gets or sets the role list.
        /// </summary>
        public List<string>? Roles { get; set; }
    }
}
