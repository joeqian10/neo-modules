namespace Neo.Plugins
{
    public class ConstructionCombineRequest
    {
        public NetworkIdentifier NetworkIdentifier { get; set; }
        public string UnsignedTransaction { get; set; }
        public Signature[] Signatures { get; set; }


    }
}
