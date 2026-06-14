using System;
using System.Collections.Generic;
using System.Linq;
using BlockChain.Models;
using Blockchain = BlockChain.Service.BlockChainService;

namespace BlockChain.Service
{
    public class BlockchainExplorer
    {
        private readonly Blockchain _blockchain;

        public BlockchainExplorer(Blockchain blockchain)
        {
            _blockchain = blockchain ?? throw new ArgumentNullException(nameof(blockchain));
        }

        public Transaction? FindTransactionById(string txId)
        {
            if (string.IsNullOrEmpty(txId)) return null;

            var txInChain = _blockchain.Chain
                .SelectMany(b => b.Transactions)
                .FirstOrDefault(t => string.Equals(t.Id, txId, StringComparison.OrdinalIgnoreCase));

            if (txInChain != null)
            {
                return txInChain;
            }

            return _blockchain.PendingTransactions
                .FirstOrDefault(t => string.Equals(t.Id, txId, StringComparison.OrdinalIgnoreCase));
        }

        public Block? FindBlockByTransactionId(string txId)
        {
            if (string.IsNullOrEmpty(txId)) return null;

            return _blockchain.Chain
                .FirstOrDefault(b => b.Transactions.Any(t => string.Equals(t.Id, txId, StringComparison.OrdinalIgnoreCase)));
        }

        public List<Transaction> GetTransactionHistory(string address)
        {
            if (string.IsNullOrEmpty(address)) return new List<Transaction>();

            return _blockchain.Chain
                .SelectMany(b => b.Transactions)
                .Where(t => string.Equals(t.From, address, StringComparison.OrdinalIgnoreCase) ||
                            string.Equals(t.To, address, StringComparison.OrdinalIgnoreCase))
                .Reverse() 
                .ToList();
        }

        public decimal GetTotalFeesEarned(string minerAddress)
        {
            if (string.IsNullOrEmpty(minerAddress)) return 0;

            return _blockchain.Chain
                .Where(b => string.Equals(b.Author, minerAddress, StringComparison.OrdinalIgnoreCase))
                .SelectMany(b => b.Transactions)
                .Sum(t => t.Fee); 
        }

        public decimal GetTotalVolume()
        {
            return _blockchain.Chain.SelectMany(b => b.Transactions).Sum(t => t.Amount);
        }

        public Transaction? GetLargestTransaction()
        {
            return _blockchain.Chain.SelectMany(b => b.Transactions).OrderByDescending(t => t.Amount).FirstOrDefault();
        }
    }
}