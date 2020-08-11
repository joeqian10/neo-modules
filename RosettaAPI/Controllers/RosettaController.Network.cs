using Microsoft.AspNetCore.Mvc;
using Neo.Ledger;
using Neo.Network.P2P;
using NeoBlock = Neo.Network.P2P.Payloads.Block;
using System.Collections.Generic;
using System.Linq;
using Neo.IO.Json;

namespace Neo.Plugins
{
    partial class RosettaController
    {
        [HttpPost("/network/list")]
        public IActionResult NetworkList(MetadataRequest request)
        {
            var magic = ProtocolSettings.Default.Magic;
            var network = magic == 7630401 ? "mainnet" : magic == 1953787457 ? "testnet" : "privatenet";
            NetworkIdentifier networkIdentifier = new NetworkIdentifier("neo", network);
            NetworkListResponse networkListResponse = new NetworkListResponse(new NetworkIdentifier[] { networkIdentifier });
            return FormatJson(networkListResponse.ToJson());
        }

        [HttpPost("/network/options")]
        public IActionResult NetworkOptions(NetworkRequest request)
        {
            Version version = new Version(RosettaApiSettings.Default.RosettaVersion, LocalNode.UserAgent);
            Allow allow = new Allow(OperationStatus.AllowedStatuses, OperationType.AllowedOperationTypes, Error.AllowedErrors, false);
            NetworkOptionsResponse networkOptionsResponse = new NetworkOptionsResponse(version, allow);
            return FormatJson(networkOptionsResponse.ToJson());
        }

        [HttpPost("/network/status")]
        public IActionResult NetworkStatus(NetworkRequest request)
        {
            long currentHeight = Blockchain.Singleton.Height;
            NeoBlock currentBlock = Blockchain.Singleton.GetBlock(Blockchain.Singleton.CurrentBlockHash);
            if (currentBlock == null)
                return FormatJson(Error.BLOCK_NOT_FOUND.ToJson());

            string currentBlockHash = currentBlock.Hash.ToString();
            long currentBlockTimestamp = currentBlock.Timestamp * 1000;

            BlockIdentifier currentBlockIdentifier = new BlockIdentifier(currentHeight, currentBlockHash);
            BlockIdentifier genesisBlockIdentifier = new BlockIdentifier(Blockchain.GenesisBlock.Index, Blockchain.GenesisBlock.Hash.ToString());

            var connected = LocalNode.Singleton.GetRemoteNodes().Select(p => new Peer(p.GetHashCode().IntToHash160String(),
                new Metadata(new Dictionary<string, JObject>
                {
                    { "connected", true.ToString().ToLower() },
                    { "address", p.Listener.ToString() },
                    { "height", p.LastBlockIndex.ToString() }
                })
            ));

            var unconnected = LocalNode.Singleton.GetUnconnectedPeers().Select(p => new Peer(p.GetHashCode().IntToHash160String(),
                new Metadata(new Dictionary<string, JObject>
                {
                    { "connected", false.ToString().ToLower() },
                    { "address", p.ToString() }
                })
            ));

            Peer[] peers = connected.Concat(unconnected).ToArray();
            NetworkStatusResponse response = new NetworkStatusResponse(currentBlockIdentifier, currentBlockTimestamp, genesisBlockIdentifier, peers);
            return FormatJson(response.ToJson());
        }
    }
}
