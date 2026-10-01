using System.Globalization;

namespace GenNumeros.ApplicationCore.Services
{
    // La forme d'un numero de compte, et rien d'autre : seize chiffres en
    // quatre tranches, XXX-XX-XXXXX-XXXXXX.
    public static class NumeroDeCompte
    {
        public const int LongueurDeLaBanque = 3;
        public const int LongueurDuSystemeAppelant = 2;
        public const int LongueurDeLaSuccursale = 5;
        public const int LongueurDuCompte = 6;

        public static bool EstUneTranche(string? valeur, int longueur)
        {
            return valeur != null
                && valeur.Length == longueur
                && valeur.All(char.IsAsciiDigit);
        }

        public static string Assembler(string banque, string systemeAppelant, string succursale, string compte)
        {
            return string.Join('-', banque, systemeAppelant, succursale, compte);
        }

        // Les deux derniers chiffres forment un nombre pair, ce qui revient
        // a tirer le dernier parmi 0, 2, 4, 6 et 8.
        public static string TirerUnCompte(Random hasard)
        {
            ArgumentNullException.ThrowIfNull(hasard);

            int debut = hasard.Next(0, 100000);
            int dernier = hasard.Next(0, 5) * 2;

            return debut.ToString("D5", CultureInfo.InvariantCulture)
                 + dernier.ToString(CultureInfo.InvariantCulture);
        }

        public static bool SeTermineParUnNombrePair(string numeroDeCompte)
        {
            ArgumentNullException.ThrowIfNull(numeroDeCompte);

            string deuxDerniers = numeroDeCompte[^LongueurDuSystemeAppelant..];

            return int.TryParse(deuxDerniers, NumberStyles.None, CultureInfo.InvariantCulture, out int valeur)
                && valeur % 2 == 0;
        }
    }
}
