using EI.SI;
using System;
using System.IO;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;
using System.Windows.Forms;

namespace Cliente.Forms
{
    public partial class CreateUser : Form
    {
        private const int PORT = 9999;
        private NetworkStream networkStream;

        private AesCryptoServiceProvider aes { get; set; }

        public CreateUser(NetworkStream networkStream, AesCryptoServiceProvider aes)
        {
            InitializeComponent();

            this.networkStream = networkStream;
            this.aes = aes;
        }

        private void buttonNewUser_Click(object sender, EventArgs e)
        {
            ProtocolSI protocolSI = new ProtocolSI();

            if (textBoxComPassword.Text == "" || textBoxPassword.Text == "" || textBoxUsername.Text == "")
            {
                MessageBox.Show("Introduza os Valores em falta!", "Erro",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            else if (textBoxPassword.Text != textBoxComPassword.Text)
            {
                MessageBox.Show("As passwords são diferentes!", "Erro",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            else
            {
                byte[] username = Encoding.UTF8.GetBytes(stringencrypter(textBoxUsername.Text));

                byte[] saltpreencrypt = GenerateSalt(8);
                byte[] saltposencrypt = byteencrypter(saltpreencrypt);

                byte[] password = Encoding.UTF8.GetBytes(textBoxPassword.Text);

                byte[] saltyPassword = byteencrypter(GenerateSaltedHash(password, saltpreencrypt));

                //start
                byte[] usernamepacket = protocolSI.Make(ProtocolSICmdType.USER_OPTION_1, username);
                networkStream.Write(usernamepacket, 0, usernamepacket.Length);

                byte[] passpacket = protocolSI.Make(ProtocolSICmdType.DATA, saltyPassword);
                networkStream.Write(passpacket, 0, passpacket.Length);

                string comfirmationreceived = "idle";
                while (comfirmationreceived == "idle")
                {
                    networkStream.Read(protocolSI.Buffer, 0, protocolSI.Buffer.Length);
                    comfirmationreceived = protocolSI.GetStringFromData();
                }

                byte[] saltpacket = protocolSI.Make(ProtocolSICmdType.ACK, saltposencrypt);
                networkStream.Write(saltpacket, 0, saltpacket.Length);

                comfirmationreceived = "idle";
                while (comfirmationreceived == "idle")
                {
                    networkStream.Read(protocolSI.Buffer, 0, protocolSI.Buffer.Length);
                    comfirmationreceived = protocolSI.GetStringFromData();
                }

                if (comfirmationreceived == "true")
                {
                    this.Close();

                    MessageBox.Show("Register was Successfull", "Register",
                        MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                else
                {
                    MessageBox.Show("Register was not Successfull", "Register",
                        MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }
        }

        private static byte[] GenerateSalt(int size)
        {
            //Generate a cryptographic random number.
            RNGCryptoServiceProvider rng = new RNGCryptoServiceProvider();
            byte[] buff = new byte[size];
            rng.GetBytes(buff);
            return buff;
        }

        private static byte[] GenerateSaltedHash(byte[] plainText, byte[] salt)
        {
            using (HashAlgorithm hashAlgorithm = SHA512.Create())
            {
                // Declarar e inicializar buffer para o texto e salt
                byte[] plainTextWithSaltBytes =
                    new byte[plainText.Length + salt.Length];

                // Copiar texto para buffer
                for (int i = 0; i < plainText.Length; i++)
                {
                    plainTextWithSaltBytes[i] = plainText[i];
                }
                // Copiar salt para buffer a seguir ao texto
                for (int i = 0; i < salt.Length; i++)
                {
                    plainTextWithSaltBytes[plainText.Length + i] = salt[i];
                }

                //Devolver hash do text + salt
                byte[] saltypass = hashAlgorithm.ComputeHash(plainTextWithSaltBytes);
                return saltypass;
            }
        }

        private byte[] byteencrypter(byte[] mensagemCifrada)
        {
            using (AesCryptoServiceProvider localaes = new AesCryptoServiceProvider())
            {
                //Partilhar key e IV
                localaes.Key = aes.Key;
                localaes.IV = aes.IV;

                //Obter dados a cifrar
                byte[] msgEmbytes = mensagemCifrada;

                //Aplicar o algoritmo de cifragem
                byte[] msgCifradaEmbytes;
                using (MemoryStream ms = new MemoryStream())
                {
                    using (CryptoStream cs = new CryptoStream(ms, localaes.CreateEncryptor(), CryptoStreamMode.Write))
                    {
                        cs.Write(msgEmbytes, 0, msgEmbytes.Length);
                        cs.FlushFinalBlock();
                    }
                    msgCifradaEmbytes = ms.ToArray();
                }

                //bytedecrypter(msgCifradaEmbytes);
                //Mostrar dados cifrados (Em Base64)
                return msgCifradaEmbytes;
            }
        }

        private string stringencrypter(string message)
        {
            //Obter dados a cifrar
            byte[] msgEmbytes = Encoding.UTF8.GetBytes(message);

            //Aplicar o algoritmo de cifragem
            byte[] msgCifradaEmbytes;
            using (MemoryStream ms = new MemoryStream())
            {
                using (CryptoStream cs = new CryptoStream(ms, aes.CreateEncryptor(), CryptoStreamMode.Write))
                {
                    cs.Write(msgEmbytes, 0, msgEmbytes.Length);
                    cs.FlushFinalBlock();
                }
                msgCifradaEmbytes = ms.ToArray();
            }

            //Mostrar dados cifrados (Em Base64)
            return Convert.ToBase64String(msgCifradaEmbytes);
        }
    }
}