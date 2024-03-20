using UnityEngine.Networking;

public class AcceptCertificateAlways : CertificateHandler
{
    protected override bool ValidateCertificate(byte[] certificateData)
    {
        return true;
    }
}
