namespace BasicFinance.Domain.Models
{
    public sealed record UserAuth(string Id, string? FullName, string? FirstName, string? LastName, string? Email);
}
