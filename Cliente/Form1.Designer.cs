namespace Cliente
{
    partial class Form1
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(Form1));
            this.openFileDialog1 = new System.Windows.Forms.OpenFileDialog();
            this.btnListaFicheiros = new System.Windows.Forms.Button();
            this.btnSendFiles = new System.Windows.Forms.Button();
            this.btnLogin = new System.Windows.Forms.Button();
            this.btnCriarCliente = new System.Windows.Forms.Button();
            this.listBoxListaFicheiros = new System.Windows.Forms.ListBox();
            this.progressBar1 = new System.Windows.Forms.ProgressBar();
            this.SuspendLayout();
            // 
            // openFileDialog1
            // 
            this.openFileDialog1.FileName = "openFileDialog1";
            // 
            // btnListaFicheiros
            // 
            this.btnListaFicheiros.BackgroundImage = global::Cliente.Properties.Resources.if_mail_check_new_47555;
            this.btnListaFicheiros.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Stretch;
            this.btnListaFicheiros.Location = new System.Drawing.Point(366, 12);
            this.btnListaFicheiros.Name = "btnListaFicheiros";
            this.btnListaFicheiros.Size = new System.Drawing.Size(112, 103);
            this.btnListaFicheiros.TabIndex = 3;
            this.btnListaFicheiros.Text = "Receber Ficheiros";
            this.btnListaFicheiros.TextAlign = System.Drawing.ContentAlignment.TopCenter;
            this.btnListaFicheiros.UseVisualStyleBackColor = true;
            this.btnListaFicheiros.Click += new System.EventHandler(this.btnListaFicheiros_Click);
            // 
            // btnSendFiles
            // 
            this.btnSendFiles.BackgroundImage = global::Cliente.Properties.Resources.if_mail_send_47557;
            this.btnSendFiles.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Stretch;
            this.btnSendFiles.Location = new System.Drawing.Point(248, 12);
            this.btnSendFiles.Name = "btnSendFiles";
            this.btnSendFiles.Size = new System.Drawing.Size(112, 103);
            this.btnSendFiles.TabIndex = 2;
            this.btnSendFiles.Text = "   Enviar     Ficheiros";
            this.btnSendFiles.TextAlign = System.Drawing.ContentAlignment.TopCenter;
            this.btnSendFiles.UseVisualStyleBackColor = true;
            this.btnSendFiles.Click += new System.EventHandler(this.btnSendFiles_Click);
            // 
            // btnLogin
            // 
            this.btnLogin.BackgroundImage = global::Cliente.Properties.Resources.if_Login_73221;
            this.btnLogin.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Center;
            this.btnLogin.Location = new System.Drawing.Point(130, 12);
            this.btnLogin.Name = "btnLogin";
            this.btnLogin.Size = new System.Drawing.Size(112, 103);
            this.btnLogin.TabIndex = 1;
            this.btnLogin.Text = "Login";
            this.btnLogin.TextAlign = System.Drawing.ContentAlignment.BottomCenter;
            this.btnLogin.UseVisualStyleBackColor = true;
            this.btnLogin.Click += new System.EventHandler(this.btnLogin_Click);
            // 
            // btnCriarCliente
            // 
            this.btnCriarCliente.BackgroundImage = global::Cliente.Properties.Resources.if_38_Target_Audience_16888381;
            this.btnCriarCliente.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Stretch;
            this.btnCriarCliente.Location = new System.Drawing.Point(12, 12);
            this.btnCriarCliente.Name = "btnCriarCliente";
            this.btnCriarCliente.Size = new System.Drawing.Size(112, 103);
            this.btnCriarCliente.TabIndex = 0;
            this.btnCriarCliente.Text = "Criar Cliente";
            this.btnCriarCliente.TextAlign = System.Drawing.ContentAlignment.BottomCenter;
            this.btnCriarCliente.UseVisualStyleBackColor = true;
            this.btnCriarCliente.Click += new System.EventHandler(this.btnCriarCliente_Click);
            // 
            // listBoxListaFicheiros
            // 
            this.listBoxListaFicheiros.FormattingEnabled = true;
            this.listBoxListaFicheiros.Location = new System.Drawing.Point(12, 132);
            this.listBoxListaFicheiros.Name = "listBoxListaFicheiros";
            this.listBoxListaFicheiros.Size = new System.Drawing.Size(466, 160);
            this.listBoxListaFicheiros.TabIndex = 4;
            // 
            // progressBar1
            // 
            this.progressBar1.Location = new System.Drawing.Point(12, 298);
            this.progressBar1.Name = "progressBar1";
            this.progressBar1.Size = new System.Drawing.Size(465, 23);
            this.progressBar1.TabIndex = 5;
            // 
            // Form1
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(489, 326);
            this.Controls.Add(this.progressBar1);
            this.Controls.Add(this.listBoxListaFicheiros);
            this.Controls.Add(this.btnListaFicheiros);
            this.Controls.Add(this.btnSendFiles);
            this.Controls.Add(this.btnLogin);
            this.Controls.Add(this.btnCriarCliente);
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.Name = "Form1";
            this.Text = "FormPrincipal";
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.Form1_FormClosing);
            this.Load += new System.EventHandler(this.Form1_Load);
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.Button btnCriarCliente;
        private System.Windows.Forms.Button btnLogin;
        private System.Windows.Forms.Button btnSendFiles;
        private System.Windows.Forms.Button btnListaFicheiros;
        private System.Windows.Forms.OpenFileDialog openFileDialog1;
        private System.Windows.Forms.ListBox listBoxListaFicheiros;
        private System.Windows.Forms.ProgressBar progressBar1;
    }
}

