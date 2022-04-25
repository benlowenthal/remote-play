
namespace waninput2
{
    partial class ServerForm
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
            this.widthText = new System.Windows.Forms.TextBox();
            this.dimensions = new System.Windows.Forms.Label();
            this.portLabel = new System.Windows.Forms.Label();
            this.portText = new System.Windows.Forms.TextBox();
            this.openButton = new System.Windows.Forms.Button();
            this.warningText = new System.Windows.Forms.Label();
            this.heightText = new System.Windows.Forms.TextBox();
            this.x = new System.Windows.Forms.Label();
            this.closeButton = new System.Windows.Forms.Button();
            this.SuspendLayout();
            // 
            // widthText
            // 
            this.widthText.Location = new System.Drawing.Point(220, 32);
            this.widthText.Name = "widthText";
            this.widthText.Size = new System.Drawing.Size(65, 23);
            this.widthText.TabIndex = 1;
            // 
            // dimensions
            // 
            this.dimensions.AutoSize = true;
            this.dimensions.Location = new System.Drawing.Point(89, 35);
            this.dimensions.Name = "dimensions";
            this.dimensions.Size = new System.Drawing.Size(109, 15);
            this.dimensions.TabIndex = 2;
            this.dimensions.Text = "Stream Dimensions";
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
            // openButton
            // 
            this.openButton.Location = new System.Drawing.Point(102, 130);
            this.openButton.Name = "openButton";
            this.openButton.Size = new System.Drawing.Size(100, 23);
            this.openButton.TabIndex = 5;
            this.openButton.Text = "Open server";
            this.openButton.UseVisualStyleBackColor = true;
            this.openButton.Click += new System.EventHandler(this.accept_Click);
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
            // heightText
            // 
            this.heightText.Location = new System.Drawing.Point(310, 32);
            this.heightText.Name = "heightText";
            this.heightText.Size = new System.Drawing.Size(65, 23);
            this.heightText.TabIndex = 7;
            // 
            // x
            // 
            this.x.AutoSize = true;
            this.x.Location = new System.Drawing.Point(291, 35);
            this.x.Name = "x";
            this.x.Size = new System.Drawing.Size(13, 15);
            this.x.TabIndex = 8;
            this.x.Text = "x";
            // 
            // closeButton
            // 
            this.closeButton.Location = new System.Drawing.Point(229, 130);
            this.closeButton.Name = "closeButton";
            this.closeButton.Size = new System.Drawing.Size(100, 23);
            this.closeButton.TabIndex = 9;
            this.closeButton.Text = "Close server";
            this.closeButton.UseVisualStyleBackColor = true;
            this.closeButton.Click += new System.EventHandler(this.closeButton_Click);
            // 
            // ServerForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(450, 201);
            this.Controls.Add(this.closeButton);
            this.Controls.Add(this.x);
            this.Controls.Add(this.heightText);
            this.Controls.Add(this.warningText);
            this.Controls.Add(this.openButton);
            this.Controls.Add(this.portLabel);
            this.Controls.Add(this.portText);
            this.Controls.Add(this.dimensions);
            this.Controls.Add(this.widthText);
            this.Name = "ServerForm";
            this.Text = "Remote Play Server Setup";
            this.Load += new System.EventHandler(this.ServerForm_Load);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Label dimensions;
        private System.Windows.Forms.TextBox widthText;
        private System.Windows.Forms.Label portLabel;
        private System.Windows.Forms.TextBox portText;
        private System.Windows.Forms.Button openButton;
        private System.Windows.Forms.Label warningText;
        private System.Windows.Forms.TextBox heightText;
        private System.Windows.Forms.Label x;
        private System.Windows.Forms.Button closeButton;
    }
}