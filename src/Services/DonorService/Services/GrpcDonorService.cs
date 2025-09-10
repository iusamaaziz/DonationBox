using DonorService.Grpc;

using Grpc.Core;

namespace DonorService.Services;

/// <summary>
/// gRPC implementation of the donor service
/// </summary>
public class GrpcDonorService : DonorService.Grpc.DonorService.DonorServiceBase
{
    private readonly IDonorService _donorService;
    private readonly IOrganizationService _organizationService;
    private readonly ILogger<GrpcDonorService> _logger;

    public GrpcDonorService(
        IDonorService donorService,
        IOrganizationService organizationService,
        ILogger<GrpcDonorService> logger)
    {
        _donorService = donorService;
        _organizationService = organizationService;
        _logger = logger;
    }

    /// <summary>
    /// Get donor profile by user ID
    /// </summary>
    public override async Task<GetDonorResponse> GetDonor(GetDonorRequest request, ServerCallContext context)
    {
        try
        {
            _logger.LogDebug("Getting donor via gRPC for user: {UserId}", request.UserId);

            var donor = await _donorService.GetDonorByUserIdAsync(request.UserId);
            if (donor == null)
            {
                return new GetDonorResponse
                {
                    Found = false,
                    ErrorMessage = "Donor not found"
                };
            }

            var donorProto = new DonorService.Grpc.Donor
            {
                UserId = donor.UserId,
                Bio = donor.Bio ?? string.Empty,
                Interests = { donor.InterestsList },
                PhoneNumber = donor.PhoneNumber ?? string.Empty,
                Address = donor.Address ?? string.Empty,
                IsActive = donor.IsActive,
                CreatedAt = ((DateTimeOffset)donor.CreatedAt).ToUnixTimeSeconds(),
                UpdatedAt = ((DateTimeOffset)donor.UpdatedAt).ToUnixTimeSeconds()
            };

            return new GetDonorResponse
            {
                Found = true,
                Donor = donorProto
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting donor via gRPC for user: {UserId}", request.UserId);
            return new GetDonorResponse
            {
                Found = false,
                ErrorMessage = "Internal server error"
            };
        }
    }

    /// <summary>
    /// Create or update donor profile
    /// </summary>
    public override async Task<CreateDonorResponse> CreateDonor(CreateDonorRequest request, ServerCallContext context)
    {
        try
        {
            _logger.LogDebug("Creating/updating donor via gRPC for user: {UserId}", request.UserId);

            var donor = await _donorService.CreateOrUpdateDonorAsync(
                request.UserId,
                request.Bio,
                request.Interests.ToList(),
                request.PhoneNumber,
                request.Address);

            var donorProto = new DonorService.Grpc.Donor
            {
                UserId = donor.UserId,
                Bio = donor.Bio ?? string.Empty,
                Interests = { donor.InterestsList },
                PhoneNumber = donor.PhoneNumber ?? string.Empty,
                Address = donor.Address ?? string.Empty,
                IsActive = donor.IsActive,
                CreatedAt = ((DateTimeOffset)donor.CreatedAt).ToUnixTimeSeconds(),
                UpdatedAt = ((DateTimeOffset)donor.UpdatedAt).ToUnixTimeSeconds()
            };

            return new CreateDonorResponse
            {
                Success = true,
                Donor = donorProto
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating/updating donor via gRPC for user: {UserId}", request.UserId);
            return new CreateDonorResponse
            {
                Success = false,
                ErrorMessage = "Internal server error"
            };
        }
    }

    /// <summary>
    /// Get donor's organizations
    /// </summary>
    public override async Task<GetDonorOrganizationsResponse> GetDonorOrganizations(GetDonorOrganizationsRequest request, ServerCallContext context)
    {
        try
        {
            _logger.LogDebug("Getting organizations via gRPC for user: {UserId}", request.UserId);

            var organizations = await _donorService.GetDonorOrganizationsAsync(request.UserId);

            var organizationProtos = organizations.Select(o => new Organization
            {
                Id = o.Id.ToString(),
                Name = o.Name,
                Description = o.Description ?? string.Empty,
                Type = o.Type,
                Mission = o.Mission ?? string.Empty,
                WebsiteUrl = o.WebsiteUrl ?? string.Empty,
                ContactEmail = o.ContactEmail ?? string.Empty,
                ContactPhone = o.ContactPhone ?? string.Empty,
                Address = o.Address ?? string.Empty,
                TaxId = o.TaxId ?? string.Empty,
                CreatedByUserId = o.CreatedByUserId,
                IsVerified = o.IsVerified,
                IsActive = o.IsActive,
                CreatedAt = ((DateTimeOffset)o.CreatedAt).ToUnixTimeSeconds(),
                UpdatedAt = ((DateTimeOffset)o.UpdatedAt).ToUnixTimeSeconds()
            });

            return new GetDonorOrganizationsResponse
            {
                Organizations = { organizationProtos }
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting organizations via gRPC for user: {UserId}", request.UserId);
            return new GetDonorOrganizationsResponse
            {
                ErrorMessage = "Internal server error"
            };
        }
    }

    /// <summary>
    /// Create a new organization
    /// </summary>
    public override async Task<CreateOrganizationResponse> CreateOrganization(CreateOrganizationRequest request, ServerCallContext context)
    {
        try
        {
            _logger.LogDebug("Creating organization via gRPC: {Name}", request.Name);

            var organization = await _organizationService.CreateOrganizationAsync(
                request.Name,
                request.Description,
                request.Type,
                request.Mission,
                request.WebsiteUrl,
                request.ContactEmail,
                request.ContactPhone,
                request.Address,
                request.TaxId,
                request.CreatedByUserId);

            var organizationProto = new Organization
            {
                Id = organization.Id.ToString(),
                Name = organization.Name,
                Description = organization.Description ?? string.Empty,
                Type = organization.Type,
                Mission = organization.Mission ?? string.Empty,
                WebsiteUrl = organization.WebsiteUrl ?? string.Empty,
                ContactEmail = organization.ContactEmail ?? string.Empty,
                ContactPhone = organization.ContactPhone ?? string.Empty,
                Address = organization.Address ?? string.Empty,
                TaxId = organization.TaxId ?? string.Empty,
                CreatedByUserId = organization.CreatedByUserId,
                IsVerified = organization.IsVerified,
                IsActive = organization.IsActive,
                CreatedAt = ((DateTimeOffset)organization.CreatedAt).ToUnixTimeSeconds(),
                UpdatedAt = ((DateTimeOffset)organization.UpdatedAt).ToUnixTimeSeconds()
            };

            return new CreateOrganizationResponse
            {
                Success = true,
                Organization = organizationProto
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating organization via gRPC: {Name}", request.Name);
            return new CreateOrganizationResponse
            {
                Success = false,
                ErrorMessage = "Internal server error"
            };
        }
    }

    /// <summary>
    /// Get organization details
    /// </summary>
    public override async Task<GetOrganizationResponse> GetOrganization(GetOrganizationRequest request, ServerCallContext context)
    {
        try
        {
            _logger.LogDebug("Getting organization via gRPC: {OrganizationId}", request.OrganizationId);

            if (!Guid.TryParse(request.OrganizationId, out var organizationId))
            {
                return new GetOrganizationResponse
                {
                    Found = false,
                    ErrorMessage = "Invalid organization ID format"
                };
            }

            var organization = await _organizationService.GetOrganizationByIdAsync(organizationId);
            if (organization == null)
            {
                return new GetOrganizationResponse
                {
                    Found = false,
                    ErrorMessage = "Organization not found"
                };
            }

            var organizationProto = new Organization
            {
                Id = organization.Id.ToString(),
                Name = organization.Name,
                Description = organization.Description ?? string.Empty,
                Type = organization.Type,
                Mission = organization.Mission ?? string.Empty,
                WebsiteUrl = organization.WebsiteUrl ?? string.Empty,
                ContactEmail = organization.ContactEmail ?? string.Empty,
                ContactPhone = organization.ContactPhone ?? string.Empty,
                Address = organization.Address ?? string.Empty,
                TaxId = organization.TaxId ?? string.Empty,
                CreatedByUserId = organization.CreatedByUserId,
                IsVerified = organization.IsVerified,
                IsActive = organization.IsActive,
                CreatedAt = ((DateTimeOffset)organization.CreatedAt).ToUnixTimeSeconds(),
                UpdatedAt = ((DateTimeOffset)organization.UpdatedAt).ToUnixTimeSeconds()
            };

            return new GetOrganizationResponse
            {
                Found = true,
                Organization = organizationProto
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting organization via gRPC: {OrganizationId}", request.OrganizationId);
            return new GetOrganizationResponse
            {
                Found = false,
                ErrorMessage = "Internal server error"
            };
        }
    }

    /// <summary>
    /// Update organization details
    /// </summary>
    public override async Task<UpdateOrganizationResponse> UpdateOrganization(UpdateOrganizationRequest request, ServerCallContext context)
    {
        try
        {
            _logger.LogDebug("Updating organization via gRPC: {OrganizationId}", request.OrganizationId);

            if (!Guid.TryParse(request.OrganizationId, out var organizationId))
            {
                return new UpdateOrganizationResponse
                {
                    Success = false,
                    ErrorMessage = "Invalid organization ID format"
                };
            }

            var organization = await _organizationService.UpdateOrganizationAsync(
                organizationId,
                request.Name,
                request.Description,
                request.Type,
                request.Mission,
                request.WebsiteUrl,
                request.ContactEmail,
                request.ContactPhone,
                request.Address,
                request.TaxId,
                request.UpdatedByUserId);

            if (organization == null)
            {
                return new UpdateOrganizationResponse
                {
                    Success = false,
                    ErrorMessage = "Organization not found or access denied"
                };
            }

            var organizationProto = new Organization
            {
                Id = organization.Id.ToString(),
                Name = organization.Name,
                Description = organization.Description ?? string.Empty,
                Type = organization.Type,
                Mission = organization.Mission ?? string.Empty,
                WebsiteUrl = organization.WebsiteUrl ?? string.Empty,
                ContactEmail = organization.ContactEmail ?? string.Empty,
                ContactPhone = organization.ContactPhone ?? string.Empty,
                Address = organization.Address ?? string.Empty,
                TaxId = organization.TaxId ?? string.Empty,
                CreatedByUserId = organization.CreatedByUserId,
                IsVerified = organization.IsVerified,
                IsActive = organization.IsActive,
                CreatedAt = ((DateTimeOffset)organization.CreatedAt).ToUnixTimeSeconds(),
                UpdatedAt = ((DateTimeOffset)organization.UpdatedAt).ToUnixTimeSeconds()
            };

            return new UpdateOrganizationResponse
            {
                Success = true,
                Organization = organizationProto
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating organization via gRPC: {OrganizationId}", request.OrganizationId);
            return new UpdateOrganizationResponse
            {
                Success = false,
                ErrorMessage = "Internal server error"
            };
        }
    }

    /// <summary>
    /// Get all organizations
    /// </summary>
    public override async Task<GetAllOrganizationsResponse> GetAllOrganizations(GetAllOrganizationsRequest request, ServerCallContext context)
    {
        try
        {
            _logger.LogDebug("Getting all organizations via gRPC - Page: {Page}, Size: {Size}",
                request.Page, request.PageSize);

            var (organizations, totalCount) = await _organizationService.GetAllOrganizationsAsync(
                request.Page,
                request.PageSize,
                request.FilterType);

            var organizationProtos = organizations.Select(o => new Organization
            {
                Id = o.Id.ToString(),
                Name = o.Name,
                Description = o.Description ?? string.Empty,
                Type = o.Type,
                Mission = o.Mission ?? string.Empty,
                WebsiteUrl = o.WebsiteUrl ?? string.Empty,
                ContactEmail = o.ContactEmail ?? string.Empty,
                ContactPhone = o.ContactPhone ?? string.Empty,
                Address = o.Address ?? string.Empty,
                TaxId = o.TaxId ?? string.Empty,
                CreatedByUserId = o.CreatedByUserId,
                IsVerified = o.IsVerified,
                IsActive = o.IsActive,
                CreatedAt = ((DateTimeOffset)o.CreatedAt).ToUnixTimeSeconds(),
                UpdatedAt = ((DateTimeOffset)o.UpdatedAt).ToUnixTimeSeconds()
            });

            return new GetAllOrganizationsResponse
            {
                Organizations = { organizationProtos },
                TotalCount = totalCount,
                Page = request.Page,
                PageSize = request.PageSize
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting all organizations via gRPC");
            return new GetAllOrganizationsResponse
            {
                ErrorMessage = "Internal server error"
            };
        }
    }

    /// <summary>
    /// Validate organization access
    /// </summary>
    public override async Task<ValidateOrganizationAccessResponse> ValidateOrganizationAccess(ValidateOrganizationAccessRequest request, ServerCallContext context)
    {
        try
        {
            _logger.LogDebug("Validating organization access via gRPC - Org: {OrganizationId}, User: {UserId}",
                request.OrganizationId, request.UserId);

            if (!Guid.TryParse(request.OrganizationId, out var organizationId))
            {
                return new ValidateOrganizationAccessResponse
                {
                    HasAccess = false,
                    ErrorMessage = "Invalid organization ID format"
                };
            }

            var hasAccess = await _organizationService.ValidateOrganizationAccessAsync(organizationId, request.UserId);

            return new ValidateOrganizationAccessResponse
            {
                HasAccess = hasAccess
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating organization access via gRPC");
            return new ValidateOrganizationAccessResponse
            {
                HasAccess = false,
                ErrorMessage = "Internal server error"
            };
        }
    }
}
