namespace BankingAssistant.Shared.Models;

public sealed record Customer(
    string CustomerId,
    string FirstName,
    string LastName,
    string Email,
    string Phone,
    string Address,
    string MemberSince);

public sealed record Account(
    string CustomerId,
    string AccountId,
    string Type,
    string Nickname,
    string Last4,
    decimal CurrentBalance,
    decimal AvailableBalance,
    string Status);

public sealed record Transaction(
    string TransactionId,
    string AccountId,
    string Date,
    string Description,
    decimal Amount,
    string Type,
    string Category,
    decimal RunningBalance);

public sealed record FaqEntry(string Question, string Answer);
