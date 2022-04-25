
namespace waninput2
{
    partial class ClientForm
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
            this.ipText = new System.Windows.Forms.TextBox();
            this.ipLabel = new System.Windows.Forms.Label();
            this.portLabel = new System.Windows.Forms.Label();
            this.portText = new System.Windows.Forms.TextBox();
            this.accept = new System.Windows.Forms.Button();
            this.warningText = new System.Windows.Forms.Label();
            this.SuspendLayout();
            // 
            // ipText
            // 
            this.ipText.Location = new System.Drawing.Point(220, 32);
            this.ipText.Name = "ipText";
            this.ipText.Size = new System.Drawing.Size(141, 23);
            this.ipText.TabIndex = 1;
            // 
            // ipLabel
            // 
            this.ipLabel.AutoSize = true;
            this.ipLabel.Location = new System.Drawing.Point(101, 35);
            this.ipLabel.Name = "ipLabel";
            this.ipLabel.Size = new System.Drawing.Size(97, 15);
            this.ipLabel.TabIndex = 2;
            this.ipLabel.Text = "Server IP Address";
            // 
            // portLabel
            // 
            this.portLabel.AutoSize = true;
            this.portLabel.Location = new System.Drawing.Point(134, 83);
            this.portLabel.Name = "portLabel";
            this.portLabel.Size = new System.Drawing.Size(64, 15);
            this.portLabel.TabIndex = 4;
            this.portLabel.Text = "Server Port";
            // 
            // portText
            // 
            this.portText.Location = new System.Drawing.Point(220, 80);
            this.portText.Name = "portText";
            this.portText.Size = new System.Drawing.Size(65, 23);
            this.portText.TabIndex = 3;
            // 
            // accept
            // 
            this.accept.Location = new System.Drawing.Point(176, 130);
            this.accept.Name = "accept";
            this.accept.Size = new System.Drawing.Size(75, 23);
            this.accept.TabIndex = 5;
            this.accept.Text = "Connect";
            this.accept.UseVisualStyleBackColor = true;
            this.accept.Click += new System.EventHandler(this.accept_Click);
            // 
            // warningText
            // 
            this.warningText.AutoSize = true;
            this.warningText.ForeColor = System.Drawing.Color.Red;
            this.warningText.Location = new System.Drawing.Point(154, 156);
            this.warningText.Name = "warningText";
            this.warningText.Size = new System.Drawing.Size(117, 15);
            this.warningText.TabIndex = 6;
            this.warningText.Text = "Invalid configuration";
            // 
            // ClientForm
            // 
            this.AcceptButton = this.accept;
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(450, 201);
            this.Controls.Add(this.warningText);
            this.Controls.Add(this.accept);
            this.Controls.Add(this.portLabel);
            this.Controls.Add(this.portText);
            this.Controls.Add(this.ipLabel);
            this.Controls.Add(this.ipText);
            this.Name = "ClientForm";
            this.Text = "Remote Play Client Setup";
            this.Load += new System.EventHandler(this.ClientForm_Load);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Label ipLabel;
        private System.Windows.Forms.TextBox ipText;
        private System.Windows.Forms.Label portLabel;
        private System.Windows.Forms.TextBox portText;
        private System.Windows.Forms.Button accept;
        private System.Windows.Forms.Label warningText;
    }
}