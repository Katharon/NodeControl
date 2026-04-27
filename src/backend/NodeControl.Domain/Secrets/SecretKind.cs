namespace NodeControl.Domain.Secrets;

public enum SecretKind
{
    Generic = 1,
    Password = 2,
    ApiToken = 3,
    SshPrivateKey = 4,
    Certificate = 5,
    ConnectionString = 6
}
