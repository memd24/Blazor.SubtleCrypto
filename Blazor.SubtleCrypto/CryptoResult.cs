namespace Blazor.SubtleCrypto
{
    public class CryptoResult
    {
        public bool Status { get; set; }
        public string Origin { get; set; }
        public string Value { get; set; }
        public Secret Secret { get; set; }
    }
    public class Secret
    {
        public string Key { get; set; }
        public string IV { get; set; }
    }
}
