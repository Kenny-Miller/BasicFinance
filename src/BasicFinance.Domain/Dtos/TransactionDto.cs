namespace BasicFinance.Domain.Dtos
{
    public record TransactionDto(Guid TransactionId, DateTimeOffset Date, decimal Amount, string Description, string Category, DateTimeOffset CreatedDate);
}
