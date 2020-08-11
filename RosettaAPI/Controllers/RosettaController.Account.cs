﻿using Microsoft.AspNetCore.Mvc;
using Neo.Ledger;
using NeoBlock = Neo.Network.P2P.Payloads.Block;
using Neo.SmartContract;
using Neo.VM;
using Neo.Wallets;
using System;
using System.Linq;
using System.Numerics;

namespace Neo.Plugins
{
    partial class RosettaController
    {
        [HttpPost("/account/balance")]
        public IActionResult AccountBalance(AccountBalanceRequest request)
        {
            if (request.AccountIdentifier is null)
                return FormatJson(Error.ACCOUNT_IDENTIFIER_INVALID.ToJson());

            UInt160 account;
            try
            {
                account = request.AccountIdentifier.Address.ToScriptHash();
            }
            catch (Exception)
            {
                return FormatJson(Error.ACCOUNT_ADDRESS_INVALID.ToJson());
            }

            // can only get current balance
            Amount[] balances = GetUtxoBalance(account);
            if (balances is null)
                return FormatJson(Error.ACCOUNT_NOT_FOUND.ToJson());

            if (request.AccountIdentifier.SubAccountIdentifier != null) // then need to get the nep5 balance
            {
                if (!UInt160.TryParse(request.AccountIdentifier.SubAccountIdentifier.Address, out UInt160 scriptHash))
                    return FormatJson(Error.CONTRACT_ADDRESS_INVALID.ToJson());
                Amount[] nep5Balances = GetNep5Balance(scriptHash, account);
                if (nep5Balances is null)
                    return FormatJson(Error.VM_FAULT.ToJson());
                balances = balances.Concat(nep5Balances).ToArray();
            }

            NeoBlock currentBlock = Blockchain.Singleton.GetBlock(Blockchain.Singleton.CurrentBlockHash);
            BlockIdentifier blockIdentifier = new BlockIdentifier(currentBlock.Index, currentBlock.Hash.ToString());
            AccountBalanceResponse response = new AccountBalanceResponse(blockIdentifier, balances);
            return FormatJson(response.ToJson());
        }

        private Amount[] GetUtxoBalance(UInt160 account)
        {
            AccountState accountState = Blockchain.Singleton.Store.GetAccounts().TryGet(account);
            if (accountState is null)
                return null;
            return accountState.Balances.Select(p => new Amount(p.Value.ToString(), new Currency(p.Key))).ToArray();
        }

        private Amount[] GetNep5Balance(UInt160 scriptHash, UInt160 account)
        {
            byte[] script;
            using (ScriptBuilder sb = new ScriptBuilder())
            {
                sb.EmitAppCall(scriptHash, "balanceOf", account);
                sb.EmitAppCall(scriptHash, "symbol");
                sb.EmitAppCall(scriptHash, "decimals");
                script = sb.ToArray();
            }

            Amount balance;
            using (ApplicationEngine engine = ApplicationEngine.Run(script, testMode:true))
            {
                if (engine.State.HasFlag(VMState.FAULT))
                    return null;
                byte decimals = (byte)engine.ResultStack.Pop().GetBigInteger();
                string symbol = engine.ResultStack.Pop().GetString();
                BigInteger amount = engine.ResultStack.Pop().GetBigInteger();
                balance = new Amount(amount.ToString(), new Currency(symbol, decimals));
            }
            return new Amount[] { balance };
        }
    }
}
