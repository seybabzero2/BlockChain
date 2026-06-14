using BlockChain.Models;
using BlockChain.Service;
using System;

var cryptoService = new CryptoService();
var blockChainService = new BlockChainService(2);
var explorer = new BlockchainExplorer(blockChainService);
var coldWallet = new ColdWalletService(cryptoService);
var networkService = new NetworkService();

var myWallet = new Wallet(cryptoService);

var seedTx = new Transaction("SYSTEM", myWallet.PublicKey, 100) { TokenSymbol = "MAIN", Fee = 0 };
TransactionService.SignTransaction(seedTx, null);
blockChainService.AddTransaction(seedTx);
blockChainService.AddBlock();

Console.WriteLine("=====================================");
Console.WriteLine("БЛОКЧЕЙН");
Console.WriteLine($"Ваша адреса: {myWallet.PublicKey}");
Console.WriteLine($"Ваш приватний ключ: {myWallet.PrivateKey}");
Console.WriteLine("=====================================\n");

while (true)
{
    Console.WriteLine("\nОберіть дію:");
    Console.WriteLine("1. Створити офлайн-транзакцію (Cold Wallet)");
    Console.WriteLine("2. Транслювати транзакцію з файлу (Online Node)");
    Console.WriteLine("3. Випустити власний токен (Mint)");
    Console.WriteLine("4. Переглянути історію гаманця (Explorer)");
    Console.WriteLine("5. Показати всі баланси");
    Console.WriteLine("6. Майнінг (Запакувати пул у блок)");
    Console.WriteLine("0. Вихід");
    Console.Write("> ");
    
    string choice = Console.ReadLine();

    try
    {
        switch (choice)
        {
            case "1":
                Console.Write("Адреса отримувача: ");
                string to = Console.ReadLine();
                Console.Write("Сума: ");
                decimal amount = decimal.Parse(Console.ReadLine());
                Console.Write("Комісія (в MAIN): ");
                decimal fee = decimal.Parse(Console.ReadLine());
                Console.Write("Символ токена (напр., MAIN): ");
                string token = Console.ReadLine();
                Console.Write("Приватний ключ: ");
                string pk = Console.ReadLine();

                var offlineTx = new Transaction(myWallet.PublicKey, to, amount) { Fee = fee, TokenSymbol = token };
                offlineTx.Signature = cryptoService.SignData(offlineTx.ToRawString(), pk);
                
                coldWallet.GenerateOfflineTransaction(myWallet.PublicKey, to, amount, pk, "offline_tx.json");
                Console.WriteLine("Файл offline_tx.json згенеровано!");
                break;

            case "2":
                Console.Write("Введіть шлях до файлу (за замовчуванням offline_tx.json): ");
                string path = Console.ReadLine();
                if (string.IsNullOrEmpty(path)) path = "offline_tx.json";
                
                networkService.BroadcastTransactionFromFile(path, blockChainService);
                break;

            case "3":
                Console.Write("Назва вашого токена (напр., ACADEMY_COIN): ");
                string symbol = Console.ReadLine();
                Console.Write("Кількість емісії: ");
                decimal mintAmount = decimal.Parse(Console.ReadLine());

                var mintTx = new Transaction("MINT", myWallet.PublicKey, mintAmount) { TokenSymbol = symbol, Fee = 0 };
                blockChainService.AddTransaction(mintTx);
                Console.WriteLine($"Запит на випуск {mintAmount} {symbol} додано в Мемпул!");
                break;

            case "4":
                var history = explorer.GetTransactionHistory(myWallet.PublicKey);
                Console.WriteLine($"\n--- Історія транзакцій ---");
                foreach (var tx in history)
                {
                    Console.WriteLine(tx.ToString());
                }
                break;

            case "5":
                var balances = blockChainService.GetBalances(myWallet.PublicKey);
                Console.WriteLine("\n--- Ваші баланси ---");
                foreach (var bal in balances)
                {
                    Console.WriteLine($"{bal.Key}: {bal.Value}");
                }
                break;

            case "6":
                blockChainService.AddBlock();
                Console.WriteLine("Блок успішно знайдено та додано!");
                break;

            case "0":
                return;

            default:
                Console.WriteLine("Невірна команда.");
                break;
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine($"[ПОМИЛКА] {ex.Message}");
    }
}