using UnityEngine.Networking;

namespace MixedWorld.Utility
{
    public class CertificationValidator : CertificateHandler
    {
        private static CertificationValidator instance = null;

        public static CertificationValidator Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = new CertificationValidator();
                }

                return instance;
            }
        }

        protected CertificationValidator() { }

        protected override bool ValidateCertificate(byte[] certificateData)
        {
            // Accept all the certificates!
            return true;
        }
    }
}
