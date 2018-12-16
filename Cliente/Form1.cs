using Cliente.Forms;
using EI.SI;
using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;
using System.Windows.Forms;

namespace Cliente
{
    public partial class Form1 : Form
    {
        private const int PORT = 9999;
        private TcpClient client;
        private NetworkStream networkStream;

        private RSACryptoServiceProvider rsa { get; set; }
        private AesCryptoServiceProvider aes { get; set; }
        private SHA1 sha1;

        public Form1()
        {
            InitializeComponent();

            IPEndPoint endpoint = new IPEndPoint(IPAddress.Loopback, PORT);
            client = new TcpClient();
            client.Connect(endpoint);
            networkStream = client.GetStream();
            rsa = new RSACryptoServiceProvider(2048);
            aes = new AesCryptoServiceProvider();
        }

        private void btnCriarCliente_Click(object sender, EventArgs e)
        {
            CreateUser FormcreateUser = new CreateUser(networkStream, aes);
            FormcreateUser.ShowDialog();
        }

        private void btnLogin_Click(object sender, EventArgs e)
        {
            Login FormLogin = new Login(networkStream, aes);
            FormLogin.ShowDialog();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            ProtocolSI protocolSI = new ProtocolSI(); //instanciar protocolo de comunicaçao

            //instaciar cript assimetrica
            string publickeystring = rsa.ToXmlString(false); //buscar chave publica

            byte[] publicKeyPacket = protocolSI.Make(ProtocolSICmdType.PUBLIC_KEY, publickeystring);
            networkStream.Write(publicKeyPacket, 0, publicKeyPacket.Length);//mandar chave publica para servidor

            while (protocolSI.GetCmdType() != ProtocolSICmdType.SECRET_KEY) //receber chave simetrica e desencriptar
            {
                networkStream.Read(protocolSI.Buffer, 0, protocolSI.Buffer.Length);
                byte[] simetrickeypacket = protocolSI.GetData();
                byte[] decryptedkey = rsa.Decrypt(simetrickeypacket, true);
                aes.Key = decryptedkey;

                //comfirmar receçao
                byte[] keycomfirmreceived = protocolSI.Make(ProtocolSICmdType.DATA, "true");
                networkStream.Write(keycomfirmreceived, 0, keycomfirmreceived.Length);
            }

            while (protocolSI.GetCmdType() != ProtocolSICmdType.IV) //receber IV e desencriptar
            {
                networkStream.Read(protocolSI.Buffer, 0, protocolSI.Buffer.Length);
                byte[] IVpacket = protocolSI.GetData();
                byte[] decryptedIV = rsa.Decrypt(IVpacket, true);
                aes.IV = decryptedIV;

                //comfirmarrececao
                byte[] IVcomfirmreceived = protocolSI.Make(ProtocolSICmdType.DATA, "true");
                networkStream.Write(IVcomfirmreceived, 0, IVcomfirmreceived.Length);
            }
        }

        private void btnSendFiles_Click(object sender, EventArgs e)
        {
            ProtocolSI protocolSI = new ProtocolSI(); //instanciar protocolo de comunicaçao

            //verificar login
            // Selecionar imagem para enviar
            OpenFileDialog openFileDialog1 = new OpenFileDialog();
            openFileDialog1.Title = "Select a File";

            string originalfilepath = string.Empty;

            if (openFileDialog1.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                originalfilepath = openFileDialog1.FileName;
            }
            else
            {
                return; //mensagem de erro/ ficheiro nao suportado
            }

            string ext = stringencrypter(Path.GetExtension(openFileDialog1.FileName));

            //definir variaveis
            int bufferSize = 100;
            byte[] buffer = new byte[bufferSize];

            int bytesRead = 0;

            //estabelecer ligacao aqui, enviar pedido de acesso, se autorizado o server recebe o tamanho do ficheiro nesta packet
            byte[] bufferSizePacket = protocolSI.Make(ProtocolSICmdType.USER_OPTION_4, bufferSize);
            networkStream.Write(bufferSizePacket, 0, bufferSizePacket.Length);

            networkStream.Read(protocolSI.Buffer, 0, protocolSI.Buffer.Length);
            if (protocolSI.GetStringFromData() == "true")
            {
                //receber resposta aqui
                byte[] file = File.ReadAllBytes(originalfilepath);
                FileStream originalFileStream = new FileStream(originalfilepath, FileMode.Open); //instanciar controlador de escrita Stream

                //meter aqui progress bar para ficar bem bonito
                progressBar1.Minimum = 0;
                progressBar1.Maximum = (int)originalFileStream.Length;
                progressBar1.Step = bufferSize;

                byte[] datahash = generatedatahash(file);
                byte[] encrypteddatahash = byteencrypter(datahash);
                byte[] signature = byteencrypter(signData(file));

                byte[] signaturepacket = protocolSI.Make(ProtocolSICmdType.DIGITAL_SIGNATURE, signature);
                networkStream.Write(signaturepacket, 0, signaturepacket.Length);

                bool cancontinue = false;
                while (cancontinue == false)
                {
                    networkStream.Read(protocolSI.Buffer, 0, protocolSI.Buffer.Length);
                    if (protocolSI.GetStringFromData() == "true")
                    {
                        cancontinue = true;
                    }
                }

                byte[] datahashpacket = protocolSI.Make(ProtocolSICmdType.ASSYM_CIPHER_DATA, encrypteddatahash);
                networkStream.Write(datahashpacket, 0, datahashpacket.Length);

                cancontinue = false;
                while (cancontinue == false)
                {
                    networkStream.Read(protocolSI.Buffer, 0, protocolSI.Buffer.Length);
                    if (protocolSI.GetStringFromData() == "true")
                    {
                        cancontinue = true;
                    }
                }

                //copiar e enviar bytesread
                while ((bytesRead = originalFileStream.Read(buffer, 0, bufferSize)) > 0) // segmentar ficheiro
                {
                    //mandar bytes lidos
                    byte[] encriptedbytes = Encoding.UTF8.GetBytes(stringencrypter(bytesRead.ToString()));
                    byte[] readbytesPacket = protocolSI.Make(ProtocolSICmdType.PADDING, encriptedbytes);
                    networkStream.Write(readbytesPacket, 0, readbytesPacket.Length);

                    //esperar por resposta de servidor ate enviar outro

                    cancontinue = false;
                    while (cancontinue == false)
                    {
                        networkStream.Read(protocolSI.Buffer, 0, protocolSI.Buffer.Length);
                        if (protocolSI.GetStringFromData() == "true")
                        {
                            cancontinue = true;
                        }
                    }

                    //mandar segmento
                    byte[] encriptedsegment = byteencrypter(buffer);
                    byte[] segmentPacket = protocolSI.Make(ProtocolSICmdType.DATA, encriptedsegment);
                    networkStream.Write(segmentPacket, 0, segmentPacket.Length);

                    cancontinue = false;
                    while (cancontinue == false)
                    {
                        networkStream.Read(protocolSI.Buffer, 0, protocolSI.Buffer.Length);
                        if (protocolSI.GetStringFromData() == "true")
                        {
                            cancontinue = true;
                        }
                    }
                    progressBar1.PerformStep();
                }

                //terminar transferencia
                byte[] finishingpacket = protocolSI.Make(ProtocolSICmdType.EOF);
                networkStream.Write(finishingpacket, 0, finishingpacket.Length);

                originalFileStream.Close();

                //send file extension
                byte[] exttPacket = protocolSI.Make(ProtocolSICmdType.DATA, ext);
                networkStream.Write(exttPacket, 0, exttPacket.Length);

                DialogResult result =  MessageBox.Show("Ficheiro enviado com sucesso", "Send Files",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);

                if (result == DialogResult.OK)
                {
                    progressBar1.Value = 0;
                }
            }
            else
            {
                MessageBox.Show("Acess not granted", "Send Files",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
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

                return msgCifradaEmbytes;
            }
        }

        private byte[] bytedecrypter(byte[] mensagemCifrada)
        {
            int bytesLidos = 0;
            byte[] msgDecifrada = new byte[mensagemCifrada.Length];

            using (AesCryptoServiceProvider localaes = new AesCryptoServiceProvider())
            {
                //Importar key e IV
                localaes.Key = aes.Key;
                localaes.IV = aes.IV;

                //Obter os dados a decifrar
                byte[] msgADecifrar = mensagemCifrada;

                using (MemoryStream ms = new MemoryStream(msgADecifrar))
                {
                    using (CryptoStream cs = new CryptoStream(ms, localaes.CreateDecryptor(), CryptoStreamMode.Read))
                    {
                        bytesLidos = cs.Read(msgDecifrada, 0, msgDecifrada.Length);
                    }
                }
            }
            byte[] returnval = new byte[bytesLidos];
            Array.Copy(msgDecifrada, returnval, bytesLidos);
            return returnval;
        }

        private byte[] generatedatahash(byte[] data)
        {
            sha1 = SHA1.Create();

            byte[] hash = sha1.ComputeHash(data);

            return hash;
        }

        private byte[] signData(byte[] data)
        {
            byte[] signature = rsa.SignData(data, sha1);

            return signature;
        }

        private void btnListaFicheiros_Click(object sender, EventArgs e)
        {
            ProtocolSI protocolSI = new ProtocolSI(); //instanciar protocolo de comunicaçao

            byte[] getfiles = protocolSI.Make(ProtocolSICmdType.USER_OPTION_3);
            networkStream.Write(getfiles, 0, getfiles.Length); //enviar pedido

            networkStream.Read(protocolSI.Buffer, 0, protocolSI.Buffer.Length);
            if (protocolSI.GetStringFromData() == "true")// comfirmar permissao de acesso
            {
                bool filesreceived = false;

                while (filesreceived == false)
                {
                    networkStream.Read(protocolSI.Buffer, 0, protocolSI.Buffer.Length); //receber lista

                    if (protocolSI.GetCmdType() == ProtocolSICmdType.DATA)
                    {
                        byte[] bytes = bytedecrypter(protocolSI.GetData());
                        string ficheiros = Encoding.UTF8.GetString(bytes);

                        listBoxListaFicheiros.Items.Clear();

                        foreach (var item in ficheiros.Split('|'))
                        {
                            listBoxListaFicheiros.Items.Add(item);
                        }

                        filesreceived = true;
                    }
                }
            }
            else
            {
                MessageBox.Show("Acess not granted", "Send Files",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            ProtocolSI protocolSI = new ProtocolSI();

            byte[] endtrasmission = protocolSI.Make(ProtocolSICmdType.EOT);
            networkStream.Write(endtrasmission, 0, endtrasmission.Length);
        }
    }
}