namespace FinGrow.Application.DTOs;

public sealed record TwoFactorSetupResponse(string Secret, string ProvisioningUri);
