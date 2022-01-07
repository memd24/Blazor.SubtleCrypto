namespace Blazor.SubtleCrypto
{
    public  class CryptoOptions
    {
        public string Key { get; set; }
        public EncryptionType Encryption { get; set; } = EncryptionType.AES_GCM;
        public AlgorithmType Algorithm { get; set; } = AlgorithmType.Sha256;
        public bool DoubleEncryption { get; set; } = false;

    }

    
}