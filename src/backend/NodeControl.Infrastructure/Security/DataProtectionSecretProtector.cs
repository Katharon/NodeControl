using Microsoft.AspNetCore.DataProtection;
using NodeControl.Application.Abstractions.Security;

namespace NodeControl.Infrastructure.Security;

public sealed class DataProtectionSecretProtector(IDataProtectionProvider dataProtectionProvider)
    : ISecretProtector
{
    private readonly IDataProtector protector = dataProtectionProvider.CreateProtector("NodeControl.Secrets.v1");

    public string Protect(string plaintext)
    {
        return protector.Protect(plaintext);
    }

    public string Unprotect(string protectedValue)
    {
        return protector.Unprotect(protectedValue);
    }
}
