using System;
using System.IO;
using System.Text.Json;
using BlockChain.Models;

namespace BlockChain.Service
{
    public class ColdWalletService
    {
        private readonly CryptoService _cryptoService;

        public ColdWalletService(CryptoService cryptoService)
        {
            _cryptoService = cryptoService;
        }

        public void GenerateOfflineTransaction(string from, string to, decimal amount, string privateKey, string filePath)
        {
            var transaction = new Transaction(from, to, amount);

            byte[] signature = _cryptoService.SignData(transaction.ToRawString(), privateKey);
            transaction.Signature = signature;

            var options = new JsonSerializerOptions { WriteIndented = true };
            string jsonString = JsonSerializer.Serialize(transaction, options);

            File.WriteAllText(filePath, jsonString);
            Console.WriteLine($"[Cold Wallet] Транзакцію успішно збережено у файл: {filePath}");
        }
    }
}