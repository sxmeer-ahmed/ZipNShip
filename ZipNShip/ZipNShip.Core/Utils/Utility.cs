using System;
using System.Security.Cryptography;
using System.Text;

namespace ZipNShip.Core
{
    public static class Utility
    {
        public static string GetPartitionKey(string input, int partitionCount = 100)
        {
            if (string.IsNullOrEmpty(input))
                throw new ArgumentException("Input cannot be null or empty.", nameof(input));

            using (var md5 = MD5.Create())
            {
                byte[] inputBytes = Encoding.UTF8.GetBytes(input);
                byte[] hashBytes = md5.ComputeHash(inputBytes);
                int hashInt = BitConverter.ToInt32(hashBytes, 0);
                int positiveHash = Math.Abs(hashInt);
                return (positiveHash % partitionCount).ToString("D2");
            }
        }
    }
}
