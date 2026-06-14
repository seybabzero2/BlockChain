using BlockChain.Models;

namespace BlockChain.Service
{
    public class BlockChainService
    {
        public List<Block> Chain { get; set; }
        public List<Transaction> PendingTransactions { get; set; } = new List<Transaction>();
        private readonly HashingService _hashingService;
        private readonly MiningService _miningService;
        private const int MinDifficulty = 1;
        private const int MaxDifficulty = 6;
        private const long TargetBlockTimeMs = 5000;
        private const int DifficultyAdjustmentInterval = 3;
        private int Difficulty;

        public BlockChainService(int difficulty)
        {
            _hashingService = new HashingService();
            _miningService = new MiningService(_hashingService);
            Chain = new List<Block>();
            Difficulty = Math.Clamp(difficulty, MinDifficulty, MaxDifficulty);
            AddGenesisBlock();
        }

        private void AddGenesisBlock()
        {
            var block = new Block(0, new List<Transaction>(), "0", DateTime.Parse("2024-06-01T00:00:00Z"));
            block.Hash = _hashingService.ComputeHash(block);
            block.DifficultyAtMining = Difficulty;
            Chain.Add(block);
        }

        public void AddBlock()
        {
            var transactions = new List<Transaction>(PendingTransactions);
            PendingTransactions.Clear();

            if (Chain.Count >= DifficultyAdjustmentInterval &&
                Chain.Count % DifficultyAdjustmentInterval == 0)
            {
                AdjustDifficulty();
            }

            var lastBlock = Chain.Last();
            var newBlock = new Block(lastBlock.Index + 1, transactions, lastBlock.Hash, DateTime.UtcNow);

            newBlock.DifficultyAtMining = Difficulty;
            newBlock.Hash = _hashingService.ComputeHash(newBlock);

            _miningService.MineBlock(newBlock, Difficulty);
            Chain.Add(newBlock);
        }

        public Dictionary<string, decimal> GetBalances(string publicKey)
        {
            var balances = new Dictionary<string, decimal>();

            Action<Transaction> processTx = (tx) =>
            {
                if (!balances.ContainsKey(tx.TokenSymbol)) balances[tx.TokenSymbol] = 0;
                if (!balances.ContainsKey("MAIN")) balances["MAIN"] = 0;

                if (tx.From == publicKey)
                {
                    balances[tx.TokenSymbol] -= tx.Amount;
                    balances["MAIN"] -= tx.Fee;
                }
                if (tx.To == publicKey)
                {
                    balances[tx.TokenSymbol] += tx.Amount;
                }
            };

            foreach (var block in Chain)
                foreach (var tx in block.Transactions)
                    processTx(tx);

            foreach (var tx in PendingTransactions)
                processTx(tx);

            return balances;
        }

        public void AddTransaction(Transaction tx)
        {
            var (isValid, error) = TransactionService.ValidateTransaction(tx);
            if (!isValid)
            {
                throw new InvalidOperationException($"Invalid transaction: {error}");
            }

            if (tx.From != "SYSTEM" && tx.From != "MINT")
            {
                var balances = GetBalances(tx.From);
                decimal tokenBalance = balances.ContainsKey(tx.TokenSymbol) ? balances[tx.TokenSymbol] : 0;
                decimal mainBalance = balances.ContainsKey("MAIN") ? balances["MAIN"] : 0;

                if (tx.TokenSymbol == "MAIN")
                {
                    if (tokenBalance < tx.Amount + tx.Fee)
                        throw new InvalidOperationException("Insufficient MAIN funds for amount + fee.");
                }
                else
                {
                    if (tokenBalance < tx.Amount)
                        throw new InvalidOperationException($"Insufficient {tx.TokenSymbol} funds.");
                    if (mainBalance < tx.Fee)
                        throw new InvalidOperationException("Insufficient MAIN funds to pay the fee.");
                }
            }

            PendingTransactions.Add(tx);
        }

        private void AdjustDifficulty()
        {
            int windowSize = DifficultyAdjustmentInterval;
            var recentBlocks = Chain.TakeLast(windowSize).ToList();

            if (recentBlocks.Count < 2)
                return;

            long totalMs = (long)(recentBlocks.Last().Timestamp - recentBlocks.First().Timestamp)
                                 .TotalMilliseconds;

            if (totalMs <= 0)
                return;

            int oldDifficulty = Difficulty;

            long targetTotal = TargetBlockTimeMs * (windowSize - 1);
            double ratio = (double)targetTotal / totalMs;
            int newDifficulty = (int)Math.Round(oldDifficulty * ratio);
            Difficulty = Math.Clamp(newDifficulty, MinDifficulty, MaxDifficulty);

            long avgMs = totalMs / (recentBlocks.Count - 1);

            if (Difficulty != oldDifficulty)
                Console.WriteLine($"\n[Difficulty adjusted: {oldDifficulty} → {Difficulty}] " +
                                  $"(avg block time: {avgMs} ms, target: {TargetBlockTimeMs} ms, ratio: {ratio:F2})");
            else
                Console.WriteLine($"\n[Difficulty unchanged: {Difficulty}] " +
                                  $"(avg block time: {avgMs} ms, target: {TargetBlockTimeMs} ms)");
        }


        public void PrintDifficultyHistory()
        {
            Console.WriteLine(new string('-', 40));
            Console.WriteLine("Difficulty History:");

            foreach (var block in Chain)
            {
                Console.WriteLine($"Block #{block.Index}: Difficulty at Mining = {block.DifficultyAtMining}");
            }
        }

        public string IsValid()
        {
            for (int i = 1; i < Chain.Count; i++)
            {
                var currentBlock = Chain[i];
                var prevBlock = Chain[i - 1];

                if (currentBlock.Hash != _hashingService.ComputeHash(currentBlock))
                {
                    return $"Error in block №[{i}]: The hash does not match the block data (Data/Timestamp/Nonce has been altered).";
                }
                if (currentBlock.PrevHash != prevBlock.Hash)
                {
                    return $"Error in block №[{i}]: The chain is broken (PreviousHash does not match the hash of the previous block).";
                }
                if (!currentBlock.Hash.ToLowerInvariant().StartsWith(new string('0', currentBlock.DifficultyAtMining)))
                {
                    return $"Error in block №[{i}]: The hash does not meet the difficulty ({currentBlock.DifficultyAtMining}) at the time of mining.";
                }
            }
            return "true";
        }
    }
}