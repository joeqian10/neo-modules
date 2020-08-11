using Akka.Actor;
using Microsoft.AspNetCore.Mvc;
using Neo.Cryptography;
using Neo.Cryptography.ECC;
using Neo.IO;
using Neo.IO.Json;
using Neo.Ledger;
using Neo.Network.P2P.Payloads;
using Neo.SmartContract;
using Neo.VM;
using Neo.Wallets;
using System;
using System.Collections.Generic;
using System.Linq;
using NeoTransaction = Neo.Network.P2P.Payloads.Transaction;

namespace Neo.Plugins
{
    partial class RosettaController
    {
        /// <summary>
        /// Combine creates a network-specific transaction from an unsigned transaction and an array of provided signatures. 
        /// The signed transaction returned from this method will be sent to the `/construction/submit` endpoint by the caller.
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        [HttpPost("/construction/combine")]
        public IActionResult ConstructionCombine(ConstructionCombineRequest request)
        {
            NeoTransaction neoTx;
            try
            {
                neoTx = NeoTransaction.DeserializeFrom(request.UnsignedTransaction.HexToBytes());
            }
            catch (Exception)
            {
                return FormatJson(Error.TX_DESERIALIZE_ERROR.ToJson());
            }

            if (neoTx.Witnesses != null && neoTx.Witnesses.Length > 0)
                return FormatJson(Error.TX_ALREADY_SIGNED.ToJson());

            if (request.Signatures.Length == 0)
                return FormatJson(Error.NO_SIGNATURE.ToJson());

            Witness witness;
            if (request.Signatures.Length == 1) // single signed
                witness = CreateSignatureWitness(request.Signatures[0]);
            else
                witness = CreateMultiSignatureWitness(request.Signatures);

            if (witness is null)
                return FormatJson(Error.INVALID_SIGNATURES.ToJson());

            neoTx.Witnesses = new Witness[] { witness };
            byte[] rawTx = neoTx.ToArray();
            ConstructionCombineResponse response = new ConstructionCombineResponse(rawTx.ToHexString());
            return FormatJson(response.ToJson());
        }

        /// <summary>
        /// Derive returns the network-specific address associated with a public key. 
        /// Blockchains that require an on-chain action to create an account should not implement this method.
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        [HttpPost("/construction/derive")]
        public IActionResult ConstructionDerive(ConstructionDeriveRequest request)
        {
            if (request.PublicKey.CurveType != CurveType.Secp256r1)
                return FormatJson(Error.CURVE_NOT_SUPPORTED.ToJson());

            ECPoint pubKey;
            try
            {
                pubKey = ECPoint.FromBytes(request.PublicKey.Bytes, ECCurve.Secp256k1);
            }
            catch (Exception)
            {
                return FormatJson(Error.INVALID_PUBLIC_KEY.ToJson());
            }

            string address = pubKey.EncodePoint(true).ToScriptHash().ToAddress();
            ConstructionDeriveResponse response = new ConstructionDeriveResponse(address);
            return FormatJson(response.ToJson());
        }

        /// <summary>
        /// Hash returns the network-specific transaction hash for a signed transaction.
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        [HttpPost("/construction/hash")]
        public IActionResult ConstructionHash(ConstructionHashRequest request)
        {
            NeoTransaction neoTx;
            try
            {
                neoTx = NeoTransaction.DeserializeFrom(request.SignedTransaction.HexToBytes());
            }
            catch (Exception)
            {
                return FormatJson(Error.TX_DESERIALIZE_ERROR.ToJson());
            }
            var hash = neoTx.Hash.ToString();
            ConstructionHashResponse response = new ConstructionHashResponse(hash);
            return FormatJson(response.ToJson());
        }

        /// <summary>
        /// Get any information required to construct a transaction for a specific network. Metadata returned here could be a recent hash to use, an account sequence number, or even arbitrary chain state. 
        /// The request used when calling this endpoint is often created by calling `/construction/preprocess` in an offline environment. 
        /// It is important to clarify that this endpoint should not pre-construct any transactions for the client (this should happen in `/construction/payloads`). 
        /// </summary>
        /// <returns></returns>
        [HttpPost("/construction/metadata")]
        public IActionResult ConstructionMetadata(ConstructionMetadataRequest request)
        {
            ConstructionMetadataResponse response = new ConstructionMetadataResponse(request.Options);
            return FormatJson(response.ToJson());
        }

        /// <summary>
        /// Parse is called on both unsigned and signed transactions to understand the intent of the formulated transaction. 
        /// This is run as a sanity check before signing (after `/construction/payloads`) and before broadcast (after `/construction/combine`).
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        [HttpPost("/construction/parse")]
        public IActionResult ConstructionParse(ConstructionParseRequest request)
        {
            NeoTransaction neoTx;
            try
            {
                neoTx = NeoTransaction.DeserializeFrom(request.Transaction.HexToBytes());
            }
            catch (Exception)
            {
                return FormatJson(Error.TX_DESERIALIZE_ERROR.ToJson());
            }

            Transaction tx = ConvertTx(neoTx);
            Operation[] operations = tx.Operations;
            string[] signers = new string[0];
            if (request.Signed)
            {
                signers = GetSignersFromWitnesses(neoTx.Witnesses);
                if (signers is null)
                    return FormatJson(Error.TX_WITNESS_INVALID.ToJson());
            }
            ConstructionParseResponse response = new ConstructionParseResponse(operations, signers, tx.Metadata);
            return FormatJson(response.ToJson());
        }

        /// <summary>
        /// Payloads is called with an array of operations and the response from `/construction/metadata`. 
        /// It returns an unsigned transaction blob and a collection of payloads that must be signed by particular addresses using a certain SignatureType. 
        /// The array of operations provided in transaction construction often times can not specify all "effects" of a transaction. 
        /// However, they can deterministically specify the "intent" of the transaction, which is sufficient for construction. 
        /// For this reason, parsing the corresponding transaction in the Data API (when it lands on chain) will contain a superset of whatever operations were provided during construction.
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        [HttpPost("/construction/payloads")]
        public IActionResult ConstructionPayloads(ConstructionPayloadsRequest request)
        {
            NeoTransaction neoTx;
            try
            {
                neoTx = ConvertOperations(request.Operations, request.Metadata);
            }
            catch (Exception)
            {
                return FormatJson(Error.PARAMETER_INVALID.ToJson());
            }
            byte[] raw = neoTx.ToArray();
            var scriptHashes = neoTx.GetScriptHashesForVerifying(Blockchain.Singleton.GetSnapshot());
            SigningPayload[] signingPayloads = new SigningPayload[scriptHashes.Length];
            for (int i = 0; i < scriptHashes.Length; i++)
            {
                signingPayloads[i] = new SigningPayload(scriptHashes[i].ToAddress(), raw);
            }
            ConstructionPayloadsResponse response = new ConstructionPayloadsResponse(raw.ToHexString(), signingPayloads);
            return FormatJson(response.ToJson());
        }

        /// <summary>
        /// Preprocess is called prior to `/construction/payloads` to construct a request for any metadata that is needed for transaction construction given (i.e. account nonce). 
        /// The request returned from this method will be used by the caller (in a different execution environment) to call the `/construction/metadata` endpoint.
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        [HttpPost("/construction/")]
        public IActionResult ConstructionPreprocess(ConstructionPreprocessRequest request)
        {
            // the metadata in request should include tx type and type specific info
            Metadata metadata = request.Metadata;
            if (metadata is null || metadata.Pairs is null || metadata.Pairs.Count == 0)
                return FormatJson(Error.TX_METADATA_INVALID.ToJson());
            if (metadata.TryGetValue("tx_type", out JObject txType))
                return FormatJson(Error.TX_METADATA_INVALID.ToJson());

            string type = txType.AsString();
            try
            {
                switch (type)
                {
                    case nameof(ClaimTransaction):
                        JArray coins = metadata["claims"] as JArray;
                        CoinReference[] coinReferences = coins.Select(p => new CoinReference()
                        {
                            PrevHash = UInt256.Parse(p["txid"].AsString()),
                            PrevIndex = ushort.Parse(p["vout"].AsString())
                        }).ToArray();
                        break;
                    case nameof(ContractTransaction):
                        break;
                    case nameof(InvocationTransaction):
                        byte[] script = metadata["script"].AsString().HexToBytes();
                        Fixed8 gas = Fixed8.Parse(metadata["gas"].AsString());
                        break;
                    case nameof(StateTransaction):
                        JArray descriptors = metadata["descriptors"] as JArray;
                        StateDescriptor[] stateDescriptors = descriptors.Select(p => new StateDescriptor()
                        {
                            Type = p["type"].TryGetEnum<StateType>(),
                            Key = p["key"].AsString().HexToBytes(),
                            Field = p["field"].AsString(),
                            Value = p["value"].AsString().HexToBytes()
                        }).ToArray();
                        break;
                    default:
                        throw new NotSupportedException();
                }
            }
            catch (Exception)
            {
                return FormatJson(Error.TX_METADATA_INVALID.ToJson());
            }
            ConstructionPreprocessResponse response = new ConstructionPreprocessResponse(request.Metadata);
            return FormatJson(response.ToJson());
        }

        /// <summary>
        /// Submit a pre-signed transaction to the node. This call should not block on the transaction being included in a block. 
        /// Rather, it should return immediately with an indication of whether or not the transaction was included in the mempool. 
        /// The transaction submission response should only return a 200 status if the submitted transaction could be included in the mempool. 
        /// Otherwise, it should return an error.
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        [HttpPost("/construction/submit")]
        public IActionResult ConstructionSubmit(ConstructionSubmitRequest request)
        {
            NeoTransaction neoTx;
            try
            {
                neoTx = NeoTransaction.DeserializeFrom(request.SignedTransaction.HexToBytes());
            }
            catch (Exception)
            {
                return FormatJson(Error.TX_DESERIALIZE_ERROR.ToJson());
            }

            NeoSystem system = new NeoSystem(Blockchain.Singleton.Store);
            RelayResultReason reason = system.Blockchain.Ask<RelayResultReason>(neoTx).Result;

            Error error;
            switch (reason)
            {
                case RelayResultReason.Succeed:
                    TransactionIdentifier transactionIdentifier = new TransactionIdentifier(neoTx.Hash.ToString());
                    ConstructionSubmitResponse response = new ConstructionSubmitResponse(transactionIdentifier);
                    return FormatJson(response.ToJson());
                case RelayResultReason.AlreadyExists:
                    error = new Error(-501, "The transaction already exists and cannot be sent repeatedly.", false);
                    return FormatJson(error.ToJson());
                case RelayResultReason.OutOfMemory:
                    error = new Error(-502, "The memory pool is full and no more transactions can be sent.", true);
                    return FormatJson(error.ToJson());
                case RelayResultReason.UnableToVerify:
                    error = new Error(-503, "The transaction cannot be verified.", false);
                    return FormatJson(error.ToJson());
                case RelayResultReason.Invalid:
                    error = new Error(-504, "The transaction is invalid.", false);
                    return FormatJson(error.ToJson());
                case RelayResultReason.PolicyFail:
                    error = new Error(-505, "One of the policy filters failed.", false);
                    return FormatJson(error.ToJson());
                default:
                    error = new Error(-500, "Unknown error.", true);
                    return FormatJson(error.ToJson());
            }
        }

        private NeoTransaction ConvertOperations(Operation[] operations, Metadata metadata)
        {
            TransactionType type = metadata["tx_type"].AsString().ToTransactionType();

            // operations only contains utxo transfers, and in a special order
            List<CoinReference> inputs = new List<CoinReference>();
            List<TransactionOutput> outputs = new List<TransactionOutput>();
            for (int i = 0; i < operations.Length; i++)
            {
                var operation = operations[i];
                // handle from ops, CoinChange field should have values
                if (operation.RelatedOperations is null)
                {
                    if (operation.CoinChange is null || operation.CoinChange.CoinAction != CoinAction.CoinSpent)
                        throw new ArgumentException();
                    var coin = operation.CoinChange;
                    inputs.Add(new CoinReference()
                    {
                        PrevHash = coin.CoinIdentifier.GetTxHash(),
                        PrevIndex = (ushort)coin.CoinIdentifier.GetIndex()
                    });
                }
                else // handle to ops, CoinChange field may be null
                {
                    string symbol = operation.Amount.Currency.Symbol.ToUpper();
                    UInt256 assetId = symbol == "NEO" ? Blockchain.GoverningToken.Hash : symbol == "GAS" ? Blockchain.UtilityToken.Hash : throw new NotSupportedException();
                    Fixed8 value = Fixed8.Parse(operation.Amount.Value);
                    UInt160 scriptHash = operation.Account.Address.ToScriptHash();
                    outputs.Add(new TransactionOutput()
                    {
                        AssetId = assetId,
                        Value = value,
                        ScriptHash = scriptHash
                    });
                }
            }

            NeoTransaction neoTx;
            // fetch exclusive data for each tx type
            switch (type)
            {
                case TransactionType.ClaimTransaction:
                    neoTx = new ClaimTransaction();
                    JArray coins = metadata["claims"] as JArray;
                    CoinReference[] coinReferences = coins.Select(p => new CoinReference()
                    {
                        PrevHash = UInt256.Parse(p["txid"].AsString()),
                        PrevIndex = ushort.Parse(p["vout"].AsString())
                    }).ToArray();
                    ((ClaimTransaction)neoTx).Claims = coinReferences;
                    break;
                case TransactionType.ContractTransaction:
                    neoTx = new ContractTransaction();
                    break;
                case TransactionType.InvocationTransaction:
                    neoTx = new InvocationTransaction();
                    byte[] script = metadata["script"].AsString().HexToBytes();
                    Fixed8 gas = Fixed8.Parse(metadata["gas"].AsString());
                    ((InvocationTransaction)neoTx).Script = script;
                    ((InvocationTransaction)neoTx).Gas = gas;
                    break;
                case TransactionType.StateTransaction:
                    neoTx = new StateTransaction();
                    JArray descriptors = metadata["descriptors"] as JArray;
                    StateDescriptor[] stateDescriptors = descriptors.Select(p => new StateDescriptor()
                    {
                        Type = p["type"].TryGetEnum<StateType>(),
                        Key = p["key"].AsString().HexToBytes(),
                        Field = p["field"].AsString(),
                        Value = p["value"].AsString().HexToBytes()
                    }).ToArray();
                    ((StateTransaction)neoTx).Descriptors = stateDescriptors;
                    break;
                default:
                    throw new NotSupportedException();
            }
            neoTx.Inputs = inputs.ToArray();
            neoTx.Outputs = outputs.ToArray();
            
            return neoTx;
        }
        
        private string[] GetSignersFromWitnesses(Witness[] witnesses)
        {
            List<ECPoint> pubKeys = new List<ECPoint>();
            try
            {
                foreach (var witness in witnesses)
                {
                    byte[] script = witness.VerificationScript;
                    if (script.IsSignatureContract())
                        pubKeys.Add(ECPoint.DecodePoint(script.Skip(1).Take(33).ToArray(), ECCurve.Secp256r1));
                    else if (script.IsMultiSigContract())
                    {
                        int i = 0;
                        switch (script[i++])
                        {
                            case 1:
                                ++i;
                                break;
                            case 2:
                                i += 2;
                                break;
                        }
                        while (script[i++] == 33)
                        {
                            pubKeys.Add(ECPoint.DecodePoint(script.Skip(i).Take(33).ToArray(), ECCurve.Secp256r1));
                            i += 33;
                        }
                    }
                    else
                        throw new NotSupportedException();
                }
            }
            catch (Exception)
            {
                return null;
            }
            return pubKeys.Select(p => p.EncodePoint(true).ToScriptHash().ToAddress()).ToArray();
        }

        private Witness CreateSignatureWitness(Signature signature)
        {
            if (!VerifyKeyAndSignature(signature, out ECPoint pubKey))
                return null;

            Witness witness = new Witness();
            using (ScriptBuilder sb = new ScriptBuilder())
            {
                sb.EmitPush(signature.Bytes);
                witness.InvocationScript = sb.ToArray();
            }
            witness.VerificationScript = Contract.CreateSignatureRedeemScript(pubKey);

            return witness;
        }

        private Witness CreateMultiSignatureWitness(Signature[] signatures)
        {
            ECPoint[] pubKeys = new ECPoint[signatures.Length];
            for (int i = 0; i < signatures.Length; i++)
            {
                if (!VerifyKeyAndSignature(signatures[i], out ECPoint pubKey))
                    return null;
                pubKeys[i] = pubKey;
            }

            // sort public keys in ascending order, also match their signature
            Dictionary<ECPoint, Signature> dic = pubKeys.Select((p, i) => new
            {
                PubKey = p,
                Sig = signatures[i]
            }).OrderBy(p => p.PubKey).ToDictionary(p => p.PubKey, p => p.Sig);

            Witness witness = new Witness();
            using (ScriptBuilder sb = new ScriptBuilder())
            {
                dic.Values.ToList().ForEach(p => sb.EmitPush(p.Bytes));
                witness.InvocationScript = sb.ToArray();
            }
            witness.VerificationScript = Contract.CreateMultiSigRedeemScript(signatures.Length, dic.Keys.ToArray());

            return witness;
        }

        private bool VerifyKeyAndSignature(Signature signature, out ECPoint pubKey)
        {
            pubKey = null;

            // 0. only support ecdsa and secp256k1 for now, hashing is sha256
            if (signature.SignatureType != SignatureType.Ecdsa || signature.PublicKey.CurveType != CurveType.Secp256r1)
                return false;

            // 1. check public key bytes
            try
            {
                pubKey = ECPoint.FromBytes(signature.PublicKey.Bytes, ECCurve.Secp256k1);
            }
            catch (Exception)
            {
                return false;
            }

            // 2. check if public key matches address
            if (pubKey.EncodePoint(true).ToScriptHash().ToAddress() != signature.SigningPayload.Address)
                return false;

            // 3. check if public key and signature matches
            return Crypto.Default.VerifySignature(signature.SigningPayload.Bytes, signature.Bytes, pubKey.EncodePoint(false));
        }

    }
}
