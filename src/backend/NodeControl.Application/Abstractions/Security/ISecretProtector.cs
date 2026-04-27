namespace NodeControl.Application.Abstractions.Security;

public interface ISecretProtector
{
    string Protect(string plaintext);
}
