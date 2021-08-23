namespace NServiceBus.Raw
{
    /// <summary>
    /// Configures if the endpoint should create queues on startup.
    /// </summary>
    public struct InfrastructureSetup
    {
        /// <summary>
        /// Additional queues to create.
        /// </summary>
        public string[] AdditionalQueues { get; }

        /// <summary>
        /// Should queues be created.
        /// </summary>
        public bool Create { get; }

        internal InfrastructureSetup(string[] additionalQueues)
        {
            Create = true;
            this.AdditionalQueues = additionalQueues;
        }

        /// <summary>
        /// Do not create queues.
        /// </summary>
        /// <returns></returns>
        public static InfrastructureSetup DoNothing() => new InfrastructureSetup();

        /// <summary>
        /// Create queues.
        /// </summary>
        /// <param name="additionalQueues">Additional queues to create (besides the input queue and the poison queue).</param>
        /// <returns></returns>
        public static InfrastructureSetup CreateQueues(string[] additionalQueues = null) => new InfrastructureSetup(additionalQueues);
    }
}