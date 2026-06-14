using System;
using System.IO;
using System.Text.Json;
using BlockChain.Models;

namespace BlockChain.Service
{
    public class NetworkService
    {
        public void BroadcastTransactionFromFile(string filePath, BlockChainService blockChainService)
        {
            Console.WriteLine($"[Online Node] Спроба зчитати транзакцію з {filePath}...");

            if (!File.Exists(filePath))
            {
                Console.WriteLine("[Помилка] Файл транзакції не знайдено.");
                return;
            }

            try
            {
                string jsonString = File.ReadAllText(filePath);
                var transaction = JsonSerializer.Deserialize<Transaction>(jsonString);

                if (transaction == null)
                {
                    Console.WriteLine("[Помилка] Не вдалося десеріалізувати транзакцію.");
                    return;
                }

                var validation = TransactionService.ValidateTransaction(transaction);
                if (!validation.isValid)
                {
                    Console.WriteLine($"[КРИТИЧНО] Перевірка підпису провалена: {validation.error}");
                    return;
                }

                var balances = blockChainService.GetBalances(transaction.From);
                decimal tokenBalance = balances.ContainsKey(transaction.TokenSymbol) ? balances[transaction.TokenSymbol] : 0;
                decimal mainBalance = balances.ContainsKey("MAIN") ? balances["MAIN"] : 0;

                if (transaction.TokenSymbol == "MAIN")
                {
                    if (tokenBalance < transaction.Amount + transaction.Fee)
                    {
                        Console.WriteLine($"[Відмова] Недостатньо MAIN. Баланс: {tokenBalance}, Необхідно: {transaction.Amount + transaction.Fee}");
                        return;
                    }
                }
                else
                {
                    if (tokenBalance < transaction.Amount)
                    {
                        Console.WriteLine($"[Відмова] Недостатньо {transaction.TokenSymbol}. Баланс: {tokenBalance}, Необхідно: {transaction.Amount}");
                        return;
                    }
                    if (mainBalance < transaction.Fee)
                    {
                        Console.WriteLine($"[Відмова] Недостатньо MAIN для оплати комісії. Баланс MAIN: {mainBalance}, Необхідно: {transaction.Fee}");
                        return;
                    }
                }

                blockChainService.AddTransaction(transaction); 

                Console.WriteLine("[Успіх] Офлайн-транзакцію успішно верифіковано та додано в Мемпул!");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Помилка обробки файлу] {ex.Message}");
            }
        }
    }
}