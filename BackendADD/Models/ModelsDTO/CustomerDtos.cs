namespace BackendADD.Dtos;

public record CreateCustomerDto(
    string FullName,
    string? Phone,
    string? Email,
    DateOnly? BirthDate,
    string? Address
);

public record UpdateCustomerDto(
    string FullName,
    string? Phone,
    string? Email,
    DateOnly? BirthDate,
    string? Address
);

public record BirthdayCheckDto(
    ulong Id,
    string FullName,
    bool IsBirthday
);