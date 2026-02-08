namespace HVO.Enterprise.Telemetry.Context.Providers
{
    /// <summary>
    /// Represents WCF request information.
    /// </summary>
    public sealed class WcfRequestInfo
    {
        /// <summary>
        /// Gets or sets the WCF action.
        /// </summary>
        public string? Action { get; set; }

        /// <summary>
        /// Gets or sets the endpoint address.
        /// </summary>
        public string? Endpoint { get; set; }

        /// <summary>
        /// Gets or sets the binding name.
        /// </summary>
        public string? Binding { get; set; }
    }
}
