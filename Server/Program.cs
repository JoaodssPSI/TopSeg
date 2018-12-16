using EI.SI;
using System;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Windows.Forms;

namespace Server
{
    internal class Program
    {
        private const int PORT = 9999;

        private static void Main(string[] args)
        {
            IPEndPoint endpoint = new IPEndPoint(IPAddress.Loopback, PORT);
            TcpListener tcpListener = new TcpListener(endpoint);
            tcpListener.Start();

            while (true)
            {
                TcpClient client = tcpListener.AcceptTcpClient();
                ClientHandler ClientHandler = new ClientHandler(client);
                ClientHandler.Handle();
            }
        }
    }

    internal class ClientHandler
    {
        private TcpClient client;
        private RSACryptoServiceProvider rsa { get; set; }

        private byte[] secretkey { get; set; }
        private byte[] IV { get; set; }

        public ClientHandler(TcpClient client)
        {
            this.client = client;
        }

        public void Handle()
        {
            Thread registthread = new Thread(registothreadHandler);
            registthread.Start();
        }

        private void registothreadHandler()
        {
            ProtocolSI protocolSI = new ProtocolSI(); //instanciar protocolo comunicacao
            NetworkStream networkStream = client.GetStream();
            rsa = new RSACryptoServiceProvider(); //instanciar metodo cryptografico assimetrico

            AesCryptoServiceProvider aes = new AesCryptoServiceProvider();//instanciar metodo cryptografico simetrico
            secretkey = aes.Key;
            IV = aes.IV;

            Console.WriteLine("Recebida uma Nova Ligacao");

            LogReg logReg = new LogReg();

            bool IsClientLoggedIn = false;

            while (protocolSI.GetCmdType() != ProtocolSICmdType.EOT)
            {
                networkStream.Read(protocolSI.Buffer, 0, protocolSI.Buffer.Length);

                //registar
                if (protocolSI.GetCmdType() == ProtocolSICmdType.USER_OPTION_1)
                {
                    string usernamepacket = null;

                    byte[] salt = null;
                    byte[] saltypass = null;

                    usernamepacket = stringdecrypter(protocolSI.GetStringFromData());

                    Console.WriteLine("Attempt to create a new client: " + usernamepacket);

                    networkStream.Read(protocolSI.Buffer, 0, protocolSI.Buffer.Length);
                    if (protocolSI.GetCmdType() == ProtocolSICmdType.DATA)
                    {
                        saltypass = bytedecrypter(protocolSI.GetData());

                        byte[] comfirmpass = protocolSI.Make(ProtocolSICmdType.DATA, "true");
                        networkStream.Write(comfirmpass, 0, comfirmpass.Length);
                    }

                    networkStream.Read(protocolSI.Buffer, 0, protocolSI.Buffer.Length);
                    if (protocolSI.GetCmdType() == ProtocolSICmdType.ACK)
                    {
                        salt = bytedecrypter(protocolSI.GetData());

                        byte[] comfirmsalt = protocolSI.Make(ProtocolSICmdType.DATA, "true");
                        networkStream.Write(comfirmsalt, 0, comfirmsalt.Length);
                    }

                    logReg.Register(usernamepacket, saltypass, salt);
                }

                //login
                if (protocolSI.GetCmdType() == ProtocolSICmdType.USER_OPTION_2)
                {
                    string usernamestring = null;
                    string pass = null;

                    usernamestring = stringdecrypter(protocolSI.GetStringFromData());

                    Console.WriteLine("Tentativa de login de: " + usernamestring);

                    networkStream.Read(protocolSI.Buffer, 0, protocolSI.Buffer.Length);
                    if (protocolSI.GetCmdType() == ProtocolSICmdType.DATA)
                    {
                        pass = stringdecrypter(protocolSI.GetStringFromData());
                    }

                    IsClientLoggedIn = logReg.VerifyLogin(usernamestring, pass);

                    byte[] comfirmlogin = protocolSI.Make(ProtocolSICmdType.DATA, IsClientLoggedIn.ToString());
                    networkStream.Write(comfirmlogin, 0, comfirmlogin.Length);
                }

                //listaficheiros
                if (protocolSI.GetCmdType() == ProtocolSICmdType.USER_OPTION_3)
                {
                    Console.WriteLine("Pedido de envio de lista de ficheiros recebidos");

                    if (IsClientLoggedIn)
                    {
                        byte[] grantaccess = protocolSI.Make(ProtocolSICmdType.USER_OPTION_9, "true");
                        networkStream.Write(grantaccess, 0, grantaccess.Length);

                        Console.WriteLine("Pedido aceite, a enviar lista");

                        DirectoryInfo
                            dinfo = new DirectoryInfo(
                                @"C:\Users\joaod\Desktop\ProjectoTopSeg\Recursos"); //diretorio com imagens
                        FileInfo[] Files = dinfo.GetFiles("."); // get all the files in the directory to the array
                        string files = "";
                        foreach (FileInfo file in Files) //guardar nomes dos ficheiros numa string
                        {
                            files = files + file.Name + "|";
                        }

                        byte[] encriptedfiles = byteencrypter(Encoding.UTF8.GetBytes(files)); //encriptar string
                        byte[] filespacket = protocolSI.Make(ProtocolSICmdType.DATA, encriptedfiles);
                        networkStream.Write(filespacket, 0, filespacket.Length);//enviar packet
                    }
                    else
                    {
                        Console.WriteLine("Pedido negado");
                        byte[] grantaccess = protocolSI.Make(ProtocolSICmdType.USER_OPTION_9, "false");
                        networkStream.Write(grantaccess, 0, grantaccess.Length);
                    }
                }

                // receber chave publica do cliente
                if (protocolSI.GetCmdType() == ProtocolSICmdType.PUBLIC_KEY)
                {
                    string Publickeypacket = protocolSI.GetStringFromData(); //recebe pacote de dados com chave publica cliente
                    rsa.FromXmlString(Publickeypacket); //importa chave publica

                    //Buscar key e IV
                    byte[] key = aes.Key;
                    byte[] IV = aes.IV;

                    //encriptar chave simetrica
                    byte[] encryptedkey = rsa.Encrypt(key, true);

                    //enviar chave simetrica
                    byte[] encryptedkeypacket = protocolSI.Make(ProtocolSICmdType.SECRET_KEY, encryptedkey);
                    networkStream.Write(encryptedkeypacket, 0, encryptedkeypacket.Length);

                    //aguardar pela resposta
                    string comfirmationreceivedkey = "idle";
                    while (comfirmationreceivedkey == "idle")
                    {
                        networkStream.Read(protocolSI.Buffer, 0, protocolSI.Buffer.Length);
                        comfirmationreceivedkey = protocolSI.GetStringFromData();
                    }

                    //encriptar chave simetrica
                    byte[] encrypedIV = rsa.Encrypt(IV, true);

                    //enviar IV
                    byte[] IVpacket = protocolSI.Make(ProtocolSICmdType.IV, encrypedIV);
                    networkStream.Write(IVpacket, 0, IVpacket.Length);

                    //aguardar pela resposta
                    string comfirmationreceivedIV = "idle";
                    while (comfirmationreceivedIV == "idle")
                    {
                        networkStream.Read(protocolSI.Buffer, 0, protocolSI.Buffer.Length);
                        comfirmationreceivedIV = protocolSI.GetStringFromData();
                    }

                    if (comfirmationreceivedIV == "true" && comfirmationreceivedkey == "true")
                    {
                        Console.WriteLine("Parametros de comunicaçao recebidos pelo utilizador");
                    }
                }

                //receber ficheiros
                if (protocolSI.GetCmdType() == ProtocolSICmdType.USER_OPTION_4)
                {
                    Console.WriteLine("Pedido de envio de ficheiros recebido");

                    if (IsClientLoggedIn)
                    {
                        byte[] grantaccess = protocolSI.Make(ProtocolSICmdType.USER_OPTION_9, "true");
                        networkStream.Write(grantaccess, 0, grantaccess.Length);

                        Console.WriteLine("Pedido de envio de ficheiros concebido");

                        int bytesread = 0;
                        byte[] signature = null;
                        byte[] datahash = null;

                        String copyFilePath = "C:\\Users\\joaod\\Desktop\\ProjectoTopSeg\\Recursos\\copyedfile";

                        if (File.Exists(copyFilePath))//verificar se o ficheiro ja existe
                        {
                            File.Delete(copyFilePath);//se sim eliminar
                        }

                        int tamanhoficheiro = protocolSI.GetIntFromData();
                        byte[] segment = new byte[tamanhoficheiro];

                        Console.WriteLine("A receber ficheiro de um cliente");

                        FileStream copyFileStream = new FileStream(copyFilePath, FileMode.Create);//instanciar controlador de leitura Stream

                        bool signaturereceived = false;
                        bool hashreceived = false;

                        while (signaturereceived == false || hashreceived == false)
                        {
                            networkStream.Read(protocolSI.Buffer, 0, protocolSI.Buffer.Length);
                            if (protocolSI.GetCmdType() == ProtocolSICmdType.DIGITAL_SIGNATURE) // nao envia assinatura
                            {
                                signature = bytedecrypter(protocolSI.GetData());
                                byte[] bytescomfPacket = protocolSI.Make(ProtocolSICmdType.DATA, "true");
                                networkStream.Write(bytescomfPacket, 0, bytescomfPacket.Length);//mandar resposta ao cliente
                                signaturereceived = true;
                            }

                            if (protocolSI.GetCmdType() == ProtocolSICmdType.ASSYM_CIPHER_DATA)
                            {
                                datahash = bytedecrypter(protocolSI.GetData());
                                byte[] bytescomfPacket = protocolSI.Make(ProtocolSICmdType.DATA, "true");
                                networkStream.Write(bytescomfPacket, 0, bytescomfPacket.Length);//mandar resposta ao cliente
                                hashreceived = true;
                            }
                        }

                        //receber pacotes com o ficheiro segmentado
                        while (protocolSI.GetCmdType() != ProtocolSICmdType.EOF)
                        {
                            networkStream.Read(protocolSI.Buffer, 0, protocolSI.Buffer.Length);

                            if (protocolSI.GetCmdType() == ProtocolSICmdType.PADDING)
                            {
                                bytesread = Int32.Parse(stringdecrypter(protocolSI.GetStringFromData()));

                                byte[] bytescomfPacket = protocolSI.Make(ProtocolSICmdType.DATA, "true");
                                networkStream.Write(bytescomfPacket, 0, bytescomfPacket.Length);//mandar resposta ao cliente
                            }

                            if (protocolSI.GetCmdType() == ProtocolSICmdType.DATA)
                            {
                                segment = bytedecrypter(protocolSI.GetData());
                                copyFileStream.Write(segment, 0, bytesread); // receber ficheiro

                                byte[] bytescomfPacket = protocolSI.Make(ProtocolSICmdType.DATA, "true");
                                networkStream.Write(bytescomfPacket, 0, bytescomfPacket.Length);//mandar resposta ao cliente

                                Console.WriteLine("A receber Dados: " + segment.Length + "b");
                            }
                        }

                        networkStream.Read(protocolSI.Buffer, 0, protocolSI.Buffer.Length);
                        string ext = stringdecrypter(protocolSI.GetStringFromData());

                        byte[] segcomfPacket = protocolSI.Make(ProtocolSICmdType.DATA, "true");
                        networkStream.Write(segcomfPacket, 0, segcomfPacket.Length);//mandar resposta ao cliente
                        Console.WriteLine("Transferencia de ficheiro terminada");
                        copyFileStream.Close();

                        bool integritystatus = verifyintegrity(datahash, File.ReadAllBytes(copyFilePath), signature);

                        string integritycheck = string.Empty;
                        if (integritystatus)
                        {
                            integritycheck = "Ficheiro dentro dos parametros de integridade";
                        }
                        else
                        {
                            integritycheck = "Ficheiro com risco de ter sido modificado";
                        }
                        Console.WriteLine("Integrity Check: " + integritycheck);

                        Random rnd = new Random(DateTime.Now.Millisecond);
                        int newfilename = rnd.Next(0, 3000); // gere um novo nome para o ficheiro

                        File.Move(copyFilePath, "C:\\Users\\joaod\\Desktop\\ProjectoTopSeg\\Recursos\\" + newfilename.ToString() + ext); //substitui ficheiros
                    }
                    else
                    {
                        Console.WriteLine("Pedido de envio de ficheiros negado");

                        byte[] grantaccess = protocolSI.Make(ProtocolSICmdType.USER_OPTION_9, "false");
                        networkStream.Write(grantaccess, 0, grantaccess.Length);
                    }
                }
            }
            Console.WriteLine("Ligaçao terminada");
        }

        private byte[] bytedecrypter(byte[] mensagemCifrada)
        {
            int bytesLidos = 0;
            byte[] msgDecifrada = new byte[mensagemCifrada.Length];

            using (AesCryptoServiceProvider localaes = new AesCryptoServiceProvider())
            {
                //Importar key e IV
                localaes.Key = this.secretkey;
                localaes.IV = this.IV;

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

        private string stringdecrypter(string message)
        {
            //Obter os dados a decifrar
            byte[] msgADecifrar = Convert.FromBase64String(message);

            //Aplicar o algoritmo de cifragem
            byte[] msgDecifrada =
                new byte[msgADecifrar.Length];
            int bytesLidos = 0;
            using (AesCryptoServiceProvider aes = new AesCryptoServiceProvider())
            {
                aes.Key = this.secretkey;
                aes.IV = this.IV;
                using (MemoryStream ms = new MemoryStream(msgADecifrar))
                {
                    using (CryptoStream cs = new CryptoStream(ms, aes.CreateDecryptor(), CryptoStreamMode.Read))
                    {
                        bytesLidos = cs.Read(msgDecifrada, 0, msgDecifrada.Length);
                        cs.Close();
                    }

                    //Mostrar dados a decifrar
                    return Encoding.UTF8.GetString(msgDecifrada, 0, bytesLidos);
                }
            }
        }

        private byte[] byteencrypter(byte[] mensagemCifrada)
        {
            using (AesCryptoServiceProvider localaes = new AesCryptoServiceProvider())
            {
                //Partilhar key e IV
                localaes.Key = this.secretkey;
                localaes.IV = this.IV;

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

        private bool verifyintegrity(byte[] hash, byte[] data, byte[] signature)
        {
            SHA1 sha1 = SHA1.Create();

            bool verifyhash = rsa.VerifyHash(hash, CryptoConfig.MapNameToOID("SHA1"), signature);
            bool verifydata = rsa.VerifyData(data, sha1, signature);

            return verifyhash && verifydata;
        }
    }

    internal class LogReg
    {
        public bool VerifyLogin(string username, string password)
        {
            SqlConnection conn = null;
            try
            {
                // Configurar ligação à Base de Dados
                conn = new SqlConnection();
                conn.ConnectionString = String.Format(@"Data Source=(LocalDB)\MSSQLLocalDB;AttachDbFilename=C:\Users\joaod\Desktop\ProjectoTopSeg\Server\Database1.mdf;Integrated Security=True");

                // Abrir ligação à Base de Dados
                conn.Open();

                // Declaração do comando SQL
                String sql = "SELECT * FROM Users WHERE Username = @username";
                SqlCommand cmd = new SqlCommand();
                cmd.CommandText = sql;

                // Declaração dos parâmetros do comando SQL
                SqlParameter param = new SqlParameter("@username", username);

                // Introduzir valor ao parâmentro registado no comando SQL
                cmd.Parameters.Add(param);

                // Associar ligação à Base de Dados ao comando a ser executado
                cmd.Connection = conn;

                // Executar comando SQL
                SqlDataReader reader = cmd.ExecuteReader();

                if (!reader.HasRows)
                {
                    throw new Exception("Error while trying to access an user");
                }

                // Ler resultado da pesquisa
                reader.Read();

                // Obter Hash (password + salt)
                byte[] saltedPasswordHashStored = (byte[])reader["SaltedPasswordHash"]; //bytes length equal to max byte length on db

                // Obter salt
                byte[] saltStored = (byte[])reader["Salt"];

                conn.Close();

                // verificar password

                byte[] binaryPassword = Encoding.UTF8.GetBytes(password);
                byte[] saltyPassword = GenerateSaltedHash(binaryPassword, saltStored);

                if (saltyPassword.SequenceEqual(saltedPasswordHashStored))
                {
                    Console.WriteLine("Cliente Logado com sucesso");

                    return true;
                }
                else
                {
                    Console.WriteLine("Erro while trying to log in");
                    return false;
                }
            }
            catch (Exception e)
            {
                MessageBox.Show("An error occurred: " + e.Message);
                return false;
            }
        }

        public void Register(string username, byte[] saltedPasswordHash, byte[] salt)
        {
            SqlConnection conn = null;
            try
            {
                // Configurar ligação à Base de Dados
                conn = new SqlConnection();
                conn.ConnectionString = String.Format(@"Data Source=(LocalDB)\MSSQLLocalDB;AttachDbFilename=C:\Users\joaod\Desktop\ProjectoTopSeg\Server\Database1.mdf;Integrated Security=True");

                // Abrir ligação à Base de Dados
                conn.Open();

                // Declaração dos parâmetros do comando SQL
                SqlParameter paramUsername = new SqlParameter("@username", username);
                SqlParameter paramPassHash = new SqlParameter("@saltedPasswordHash", saltedPasswordHash);
                SqlParameter paramSalt = new SqlParameter("@salt", salt);

                // Declaração do comando SQL
                String sql = "INSERT INTO Users (Username, SaltedPasswordHash, Salt) VALUES (@username,@saltedPasswordHash,@salt)";

                // Prepara comando SQL para ser executado na Base de Dados
                SqlCommand cmd = new SqlCommand(sql, conn);

                // Introduzir valores aos parâmentros registados no comando SQL
                cmd.Parameters.Add(paramUsername);
                cmd.Parameters.Add(paramPassHash);
                cmd.Parameters.Add(paramSalt);

                // Executar comando SQL
                int lines = cmd.ExecuteNonQuery();

                // Fechar ligação
                conn.Close();
                if (lines == 0)
                {
                    // Se forem devolvidas 0 linhas alteradas então o não foi executado com sucesso
                    throw new Exception("Error while inserting an user");
                }
            }
            catch (Exception e)
            {
                throw new Exception("Error while inserting an user:" + e.Message);
            }

            Console.WriteLine("New Client Registered Successfully");
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
                return hashAlgorithm.ComputeHash(plainTextWithSaltBytes);
            }
        }
    }
}