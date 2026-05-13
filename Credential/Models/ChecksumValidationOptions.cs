namespace Credential.Models
{
    public class ChecksumValidationOptions
    {
        public bool Enabled { get; set; }
        public bool EnableWeb { get; set; }
        public bool EnableMobile { get; set; }

        public bool? WebRequireChecksumForGet { get; set; }
        public bool? WebRequireChecksumForNonGet { get; set; }
        public bool? MobileRequireChecksumForGet { get; set; }
        public bool? MobileRequireChecksumForNonGet { get; set; }

        public bool EnableReplayAttackProtection { get; set; }

        public int NonceTtlSeconds { get; set; }

        public int AllowedTimestampDriftSeconds { get; set; }
    }
}
