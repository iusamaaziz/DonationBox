namespace OrganizationService.Services;

public interface IAuthValidationService
{
    Task<ValidateTokenResponse?> GetUserFromAuthHeaderAsync(string? authHeader);
}

public class ValidateTokenResponse
{
    public bool IsValid { get; set; }
    public Guid? UserId { get; set; }
    public string? Email { get; set; }
    public string? FullName { get; set; }
    public string? ErrorMessage { get; set; }
}
