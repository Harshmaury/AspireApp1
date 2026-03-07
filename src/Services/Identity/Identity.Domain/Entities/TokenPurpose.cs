// src/Services/Identity/Identity.Domain/Entities/TokenPurpose.cs
namespace Identity.Domain.Entities;

public enum TokenPurpose
{
    EmailVerification = 1,
    PasswordReset     = 2,
    MfaSetup          = 3
}
