namespace FinGrow.Application.Features.Employees.CreateEmployee;

using System.Security.Cryptography;

internal static class TemporaryPassword
{
    public const int Length = 12;

    private const string Uppercase = "ABCDEFGHJKLMNPQRSTUVWXYZ";
    private const string Lowercase = "abcdefghijkmnpqrstuvwxyz";
    private const string Digits = "23456789";
    private const string Alphabet = Uppercase + Lowercase + Digits;

    public static string Generate()
    {
        while (true)
        {
            var candidate = new string(RandomNumberGenerator.GetItems<char>(Alphabet, Length));

            if (candidate.Any(Uppercase.Contains) && candidate.Any(Lowercase.Contains) && candidate.Any(Digits.Contains))
            {
                return candidate;
            }
        }
    }
}
