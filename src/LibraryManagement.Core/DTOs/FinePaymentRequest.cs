using LibraryManagement.Core.Enums;

namespace LibraryManagement.Core.DTOs;

public sealed record FinePaymentRequest(
    int FineId,
    decimal Amount,
    PaymentMethod PaymentMethod,
    string? Notes = null);

public sealed record FineWaiveRequest(
    int FineId,
    string Reason);
