using Neo.IO.Json;

namespace Neo.Plugins
{
    // Signature contains the payload that was signed, the public keys of the keypairs used to
    // produce the signature, the signature (encoded in hex), and the SignatureType. PublicKey is often
    // times not known during construction of the signing payloads but may be needed to combine
    // signatures properly.
    public class Signature
    {
        public SigningPayload SigningPayload { get; set; }
        public PublicKey PublicKey { get; set; }
        public SignatureType SignatureType { get; set; }
        public byte[] Bytes { get; set; }

        public Signature(SigningPayload signingPayload, PublicKey publicKey, SignatureType signatureType, byte[] bytes)
        {
            SigningPayload = signingPayload;
            PublicKey = publicKey;
            SignatureType = signatureType;
            Bytes = bytes;
        }

        public JObject ToJson()
        {
            JObject json = new JObject();
            json["signing_payload"] = SigningPayload.ToJson();
            json["public_key"] = PublicKey.ToJson();
            json["signature_type"] = SignatureType.AsString();
            json["hex_bytes"] = Bytes.ToHexString();
            return json;
        }
    }
}
