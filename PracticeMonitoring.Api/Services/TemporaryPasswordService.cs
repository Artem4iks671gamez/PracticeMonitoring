using System.Security.Cryptography;

namespace PracticeMonitoring.Api.Services;

public class TemporaryPasswordService
{
    private const string Upper = "ABCDEFGHJKLMNPQRSTUVWXYZ";
    private const string Lower = "abcdefghijkmnopqrstuvwxyz";
    private const string Digits = "23456789";
    private const string All = Upper + Lower + Digits;

    public string Generate()
    {
        var chars = new List<char>
        {
            Pick(Upper),
            Pick(Lower),
            Pick(Digits)
        };

        while (chars.Count < 12)
            chars.Add(Pick(All));

        return new string(chars.OrderBy(_ => RandomNumberGenerator.GetInt32(int.MaxValue)).ToArray());
    }

    private static char Pick(string source) => source[RandomNumberGenerator.GetInt32(source.Length)];
}
