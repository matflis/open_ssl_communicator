﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using SystemSecurity;

namespace DiffieHellman
{
    public class DiffieHellmanTunnel
    {
        private byte[] PublicKey { get; set; }
        private byte[] TunnelKey { get; set; }
        private Aes aes = new AesCryptoServiceProvider();

        public DiffieHellmanTunnelStatus Status { get; set; }

        ECDiffieHellmanCng diffie = new ECDiffieHellmanCng();

        public DiffieHellmanTunnel()
        {
            diffie.KeyDerivationFunction = ECDiffieHellmanKeyDerivationFunction.Hash;
            diffie.HashAlgorithm = CngAlgorithm.Sha256;
            PublicKey = diffie.PublicKey.ToByteArray();
            Status = DiffieHellmanTunnelStatus.NOT_ESTABLISHED;
        }

        public void CreateKey(string publicPart)
        {
            byte[] bytePublicPart = Convert.FromBase64String(publicPart);
            TunnelKey = diffie.DeriveKeyMaterial(CngKey.Import(bytePublicPart, CngKeyBlobFormat.EccPublicBlob));
            aes.Key = TunnelKey;
        }

        public void loadIV(string iv)
        {
            aes.IV = Convert.FromBase64String(iv);
        }

        public string getPublicPart()
        {
            return Convert.ToBase64String(PublicKey);
        }

        public string getIV()
        {
            return Convert.ToBase64String(aes.IV);
        }

        public string diffieEncrypt(string message)
        {
            string encryptedMessage = string.Empty;

            using (MemoryStream ciphertext = new MemoryStream())
            using (CryptoStream cs = new CryptoStream(ciphertext, aes.CreateEncryptor(), CryptoStreamMode.Write))
            {
                byte[] plaintextMessage = Encoding.UTF8.GetBytes(message);
                cs.Write(plaintextMessage, 0, plaintextMessage.Length);
                cs.Close();
                encryptedMessage = Convert.ToBase64String(ciphertext.ToArray());
            }
            
            return encryptedMessage;
        }

        public string diffieDecrypt(string cipher)
        {
            string decryptedMessage = string.Empty;
            byte[] cipherMessage = Convert.FromBase64String(cipher);

            using (MemoryStream plaintext = new MemoryStream())
            using (CryptoStream cs = new CryptoStream(plaintext, aes.CreateDecryptor(), CryptoStreamMode.Write))
            {
                    cs.Write(cipherMessage, 0, cipherMessage.Length);
                    cs.Close();
                    decryptedMessage = Encoding.UTF8.GetString(plaintext.ToArray());
            }

            return decryptedMessage;
        }
    }
}
