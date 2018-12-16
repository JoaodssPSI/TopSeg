using EI.SI;
using System;
using System.IO;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;
using System.Windows.Forms;

namespace Cliente.Forms
{
    public partial class Login : Form
    {
        private const int PORT = 9999;
        private NetworkStream networkStream;

        private AesCryptoServiceProvider aes { get; set; }

        public Login(NetworkStream networkStream, AesCryptoServiceProvider aes)
        {
            InitializeComponent();
            this.networkStream = networkStream;
            this.aes = aes;
        }

        private void buttonLogin_Click(object sender, EventArgs e)
        {
            ProtocolSI protocolSI = new ProtocolSI();

            if (textBoxPassword.Text == "" || textBoxUsername.Text == "")
            {
                MessageBox.Show("Introduza os Valores em falta!", "Erro",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            else
            {
                byte[] username = Encoding.UTF8.GetBytes(stringencrypter(textBoxUsername.Text));

                byte[] password = Encoding.UTF8.GetBytes(stringencrypter(textBoxPassword.Text));

                byte[] userpacket = protocolSI.Make(ProtocolSICmdType.USER_OPTION_2, username);
                networkStream.Write(userpacket, 0, userpacket.Length);

                byte[] passpacket = protocolSI.Make(ProtocolSICmdType.DATA, password);
                networkStream.Write(passpacket, 0, passpacket.Length);

                string comfirmationreceived = "idle";
                while (comfirmationreceived == "idle")
                {
                    networkStream.Read(protocolSI.Buffer, 0, protocolSI.Buffer.Length);
                    comfirmationreceived = protocolSI.GetStringFromData();
                }

                if (comfirmationreceived == "True")
                {
                    this.Close();

                    MessageBox.Show("Login was Successfull", "Login",
                        MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                else
                {
                    MessageBox.Show("Login was not Successfull", "Login",
                        MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
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