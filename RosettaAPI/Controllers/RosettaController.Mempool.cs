using Microsoft.AspNetCore.Mvc;
using Neo.Ledger;
using System.Linq;
using NeoTransaction = Neo.Network.P2P.Payloads.Transaction;

namespace Neo.Plugins
{
    partial class RosettaController
    {
        [HttpPost("/mempool")]
        public IActionResult Mempool(NetworkRequest request)
        {
            NeoTransaction[] neoTxes = Blockchain.Singleton.MemPool.ToArray();
            TransactionIdentifier[] transactionIdentifiers = neoTxes.Select(p => new TransactionIdentifier(p.Hash.ToString())).ToArray();
            MempoolResponse response = new MempoolResponse(transactionIdentifiers);
            return FormatJson(response.ToJson());
        }

        [HttpPost("/mempool/transaction")]
        public IActionResult MempoolTransaction(MempoolTransactionRequest request)
        {
            // check tx
            if (request.TransactionIdentifier == null)
                return FormatJson(Error.TX_IDENTIFIER_INVALID.ToJson());
            if (!UInt256.TryParse(request.TransactionIdentifier.Hash, out UInt256 txHash))
                return FormatJson(Error.TX_HASH_INVALID.ToJson());
            NeoTransaction neoTx = Blockchain.Singleton.MemPool.ToArray().FirstOrDefault(p => p.Hash == txHash);
            if (neoTx == default(NeoTransaction))
                return FormatJson(Error.TX_NOT_FOUND.ToJson());

            Transaction tx = ConvertTx(neoTx);
            MempoolTransactionResponse response = new MempoolTransactionResponse(tx);
            return FormatJson(response.ToJson());
        }
    }
}
