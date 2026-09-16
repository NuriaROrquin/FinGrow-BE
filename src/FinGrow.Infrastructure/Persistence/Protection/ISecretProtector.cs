namespace FinGrow.Infrastructure.Persistence.Protection;

public interface ISecretProtector
{
    string Protect(string plaintext);

    string Unprotect(string ciphertext);
}
