public class CredentialRequest
{
    public string HolderSeed { get; set; }
    public string IssuerSeed { get; set; }
    public string RlcUrl { get; set; }
    public int counter { get; set; }
    public string flag { get; set; }
    public Data Data { get; set; }
    public MetaData MetaData { get; set; }
}

public class Data
{
    //public string dateOfBirth { get; set; }
    public string name { get; set; }
   // public string email { get; set; }
    //public string mobileNumber { get; set; }
    //public string gender { get; set; }
    //public string country { get; set; }
    public string profileType { get; set; }
}

public class MetaData
{
    public string expirationDate { get; set; }
    public string schemaURL { get; set; }
    public string name { get; set; }
    public string description { get; set; }
}
