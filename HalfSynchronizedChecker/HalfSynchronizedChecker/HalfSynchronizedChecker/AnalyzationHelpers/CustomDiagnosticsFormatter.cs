

namespace HalfSynchronizedChecker.AnalyzationHelpers
{
    public static class CustomDiagnosticsFormatter
    {
        private const string DeclarationSubString = "Declaration";
        public static string GetKindRepresentation(string kindText)
        {
            var index = kindText.IndexOf(DeclarationSubString);
            return index < 0
                ? kindText
                : kindText.Remove(index, DeclarationSubString.Length);

        }
    }
}
