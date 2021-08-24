﻿using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace PolyDeploy.Encryption
{
    // This implementation is almost entirely based on the answer found on Stack Overflow here:
    // https://stackoverflow.com/questions/10168240/encrypting-decrypting-a-string-in-c-sharp
    //
    // I've added methods to support streams.

    public class Crypto
    {
        // Used to determine the keysize of the encryption algorithm in bits.
        // This is divided by 8 later to get the equivalent number of bytes.
        private const int KeySize = 256;

        // The AES specification states that the block size must be 128.
        private const int BlockSize = 128;

        // Initialisation vector size.
        private const int IvSize = 128;

        // Salt size.
        private const int SaltSize = 256;

        // Determines the number of iterations used during password generation.
        private const int DerivationIterations = 1000;

        public static Stream Encrypt(Stream plainStream, string passPhrase)
        {
            // Read bytes from stream.
            byte[] plainBytes;

            using (MemoryStream ms = new MemoryStream())
            {
                byte[] buffer = new byte[2048];
                int bytesRead;

                while ((bytesRead = plainStream.Read(buffer, 0, buffer.Length)) > 0)
                {
                    ms.Write(buffer, 0, bytesRead);
                }

                plainBytes = ms.ToArray();
            }

            // Encrypt bytes.
            byte[] encryptedBytes = Encrypt(plainBytes, passPhrase);

            // Create stream and return.
            return new MemoryStream(encryptedBytes);
        }

        public static string Encrypt(string plainText, string passPhrase)
        {
            // Read string as bytes.
            byte[] plainBytes = Encoding.UTF8.GetBytes(plainText);

            // Encrypt bytes.
            byte[] encryptedBytes = Encrypt(plainBytes, passPhrase);

            // Convert back to string and return.
            return Convert.ToBase64String(encryptedBytes);
        }

        public static byte[] Encrypt(byte[] plainBytes, string passPhrase)
        {
            // Bytes for salt and initialisation vector are generated randomly each time.
            byte[] saltBytes = GenerateRandomEntropy(SaltSize);
            byte[] ivBytes = GenerateRandomEntropy(IvSize);

            // Prepare store for encrypted bytes.
            byte[] encryptedBytes;

            using (Rfc2898DeriveBytes password = new Rfc2898DeriveBytes(passPhrase, saltBytes, DerivationIterations))
            {
                byte[] keyBytes = password.GetBytes(KeySize / 8);

                using (AesManaged symmetricKey = new AesManaged())
                {
                    symmetricKey.BlockSize = BlockSize;
                    symmetricKey.Mode = CipherMode.CBC;
                    symmetricKey.Padding = PaddingMode.PKCS7;

                    using (ICryptoTransform encryptor = symmetricKey.CreateEncryptor(keyBytes, ivBytes))
                    {
                        using (MemoryStream memoryStream = new MemoryStream())
                        {
                            using (CryptoStream cryptoStream = new CryptoStream(memoryStream, encryptor, CryptoStreamMode.Write))
                            {
                                cryptoStream.Write(plainBytes, 0, plainBytes.Length);
                                cryptoStream.FlushFinalBlock();

                                // Initialise cipher bytes with the salt.
                                byte[] cipherBytes = saltBytes;

                                // Add the initialisation vector bytes.
                                cipherBytes = cipherBytes.Concat(ivBytes).ToArray();

                                // Finally add the encrypted data.
                                cipherBytes = cipherBytes.Concat(memoryStream.ToArray()).ToArray();

                                // Store encrypted bytes.
                                encryptedBytes = cipherBytes;
                            }
                        }
                    }
                }
            }

            return encryptedBytes;
        }

        public static Stream Decrypt(Stream encryptedStream, string passPhrase)
        {
            // Read bytes from stream.
            byte[] encryptedBytes;

            using (MemoryStream ms = new MemoryStream())
            {
                byte[] buffer = new byte[2048];
                int bytesRead;

                while ((bytesRead = encryptedStream.Read(buffer, 0, buffer.Length)) > 0)
                {
                    ms.Write(buffer, 0, bytesRead);
                }

                encryptedBytes = ms.ToArray();
            }

            // Decrypt bytes.
            byte[] plainBytes = Decrypt(encryptedBytes, passPhrase);

            // Create stream and return.
            return new MemoryStream(plainBytes);
        }

        public static string Decrypt(string encryptedText, string passPhrase)
        {
            // Read string as bytes.
            byte[] encryptedBytes = Convert.FromBase64String(encryptedText);

            // Decrypt bytes.
            byte[] plainBytes = Decrypt(encryptedBytes, passPhrase);

            // Convert to string and return.
            return Encoding.UTF8.GetString(plainBytes);
        }

        public static byte[] Decrypt(byte[] encryptedBytesWithSaltAndIv, string passPhrase)
        {
            // Get the salt bytes by extracting the first (SaltSize / 8) bytes.
            byte[] saltBytes = encryptedBytesWithSaltAndIv
                .Take(SaltSize / 8)
                .ToArray();

            // Get the initialisation vector bytes by extracting the next (IvSize / 8) bytes after the salt.
            byte[] ivBytes = encryptedBytesWithSaltAndIv
                .Skip(SaltSize / 8)
                .Take(IvSize / 8)
                .ToArray();

            // Get the actual encrypted bytes by removing the salt and iv bytes.
            byte[] encryptedBytes = encryptedBytesWithSaltAndIv
                .Skip((SaltSize / 8) + (IvSize / 8))
                .Take(encryptedBytesWithSaltAndIv.Length - ((SaltSize / 8) + (IvSize / 8)))
                .ToArray();

            // Prepare store for decrypted string and bytes read.
            byte[] plainTextBytes;
            int decryptedByteCount;

            using (Rfc2898DeriveBytes password = new Rfc2898DeriveBytes(passPhrase, saltBytes, DerivationIterations))
            {
                byte[] keyBytes = password.GetBytes(KeySize / 8);

                using (AesManaged symmetricKey = new AesManaged())
                {
                    symmetricKey.BlockSize = BlockSize;
                    symmetricKey.Mode = CipherMode.CBC;
                    symmetricKey.Padding = PaddingMode.PKCS7;

                    using (ICryptoTransform decryptor = symmetricKey.CreateDecryptor(keyBytes, ivBytes))
                    {
                        using (MemoryStream memoryStream = new MemoryStream(encryptedBytes))
                        {
                            using (CryptoStream cryptoStream = new CryptoStream(memoryStream, decryptor, CryptoStreamMode.Read))
                            {
                                byte[] decryptedBytes = new byte[encryptedBytes.Length];
                                int bytesDecrypted = cryptoStream.Read(decryptedBytes, 0, decryptedBytes.Length);

                                plainTextBytes = decryptedBytes;
                                decryptedByteCount = bytesDecrypted;
                            }
                        }
                    }
                }
            }

            return plainTextBytes.Take(decryptedByteCount).ToArray();
        }

        private static byte[] GenerateRandomEntropy(int bitCount)
        {
            byte[] randomBytes = CryptoUtilities.GenerateRandomBytes(bitCount / 8);

            return randomBytes;
        }
    }
}
