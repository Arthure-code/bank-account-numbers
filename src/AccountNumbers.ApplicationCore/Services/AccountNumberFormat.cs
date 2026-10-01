using System.Globalization;

namespace AccountNumbers.ApplicationCore.Services
{
    // The shape of an account number, and nothing else: sixteen digits in
    // four slices, XXX-XX-XXXXX-XXXXXX.
    public static class AccountNumberFormat
    {
        public const int BankLength = 3;
        public const int CallingSystemLength = 2;
        public const int BranchLength = 5;
        public const int AccountLength = 6;

        public static bool IsSlice(string? value, int length)
        {
            return value != null
                && value.Length == length
                && value.All(char.IsAsciiDigit);
        }

        public static string Compose(string bank, string callingSystem, string branch, string compte)
        {
            return string.Join('-', bank, callingSystem, branch, compte);
        }

        // The last two digits form an even number, which comes down to
        // drawing the last one among 0, 2, 4, 6 and 8.
        public static string DrawAccount(Random random)
        {
            ArgumentNullException.ThrowIfNull(random);

            int start = random.Next(0, 100000);
            int dernier = random.Next(0, 5) * 2;

            return start.ToString("D5", CultureInfo.InvariantCulture)
                 + dernier.ToString(CultureInfo.InvariantCulture);
        }

        public static bool EndsWithAnEvenNumber(string accountNumber)
        {
            ArgumentNullException.ThrowIfNull(accountNumber);

            string lastTwo = accountNumber[^CallingSystemLength..];

            return int.TryParse(lastTwo, NumberStyles.None, CultureInfo.InvariantCulture, out int value)
                && value % 2 == 0;
        }
    }
}
