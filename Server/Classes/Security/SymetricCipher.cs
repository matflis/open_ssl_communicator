﻿using OpenSSL.Crypto;
using System.Text;

namespace SystemSecurity
{
    class SymetricCipher
    {
        private readonly CipherContext cc;

        public SymetricCipher()
        {
            cc = new CipherContext(Cipher.AES_256_ECB);
        }

        public SymetricCipher(CIPHER_TYPE type, CIPHER_MODE mode, CIPHER_SIZE size)
        {
            if (type == CIPHER_TYPE.AES)
            {
                if (size == CIPHER_SIZE.SIZE_128)
                {
                    if (mode == CIPHER_MODE.CBC)
                        cc = new CipherContext(Cipher.AES_128_CBC);
                    else
                        cc = new CipherContext(Cipher.AES_128_ECB);
                }
                else if (size == CIPHER_SIZE.SIZE_192)
                {
                    if (mode == CIPHER_MODE.CBC)
                        cc = new CipherContext(Cipher.AES_192_CBC);
                    else
                        cc = new CipherContext(Cipher.AES_192_ECB);
                }
                else if (size == CIPHER_SIZE.SIZE_256)
                {
                    if (mode == CIPHER_MODE.CBC)
                        cc = new CipherContext(Cipher.AES_256_CBC);
                    else
                        cc = new CipherContext(Cipher.AES_256_ECB);
                }
            }
            else if (type == CIPHER_TYPE.BLOWFISH)
            {
                if (mode == CIPHER_MODE.CBC)
                    cc = new CipherContext(Cipher.Blowfish_CBC);
                else
                    cc = new CipherContext(Cipher.Blowfish_ECB);
            }
            else if (type == CIPHER_TYPE.CAST5)
            {
                if (mode == CIPHER_MODE.CBC)
                    cc = new CipherContext(Cipher.Cast5_CBC);
                else
                    cc = new CipherContext(Cipher.Cast5_ECB);
            }
        }

        public byte[] encode(byte[] message, string key, string iv)
        {
            try
            {
                return cc.Crypt(message, Encoding.ASCII.GetBytes(key), Encoding.ASCII.GetBytes(iv), true);
            }
            catch
            {
                return null;
            }
        }

        public byte[] decode(byte[] cipher, string key, string iv)
        {
            try
            {
                return cc.Decrypt(cipher, Encoding.ASCII.GetBytes(key), Encoding.ASCII.GetBytes(iv));
            }
            catch
            {
                return null;
            }
        }
    }
}