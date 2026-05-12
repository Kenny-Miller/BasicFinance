namespace BasicFinance.Infrastructure.Entities
{
    public interface IEntity
    {
        /// <summary>
        /// Gets a value indicating the unique identifier of the entity.
        /// </summary>
        public Guid Id { get; }

        /// <summary>
        /// Gets a value indicating when the entity was created in the system.
        /// </summary>
        public DateTimeOffset SystemCreatedDate { get; init; }

        /// <summary>
        /// Gets or sets a value indicating when the entity was last modified in the system.
        /// </summary>
        public DateTimeOffset? SystemModifiedDate { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the entity is active.
        /// </summary>
        public bool IsActive { get; set; }
    }
}
