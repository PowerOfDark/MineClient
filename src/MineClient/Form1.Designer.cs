using System.Drawing;
using System.Windows.Forms;
namespace MineClient
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
            this.startBtn = new System.Windows.Forms.Button();
            this.usernameInput = new System.Windows.Forms.TextBox();
            this.uninstallBtn = new System.Windows.Forms.Button();
            this.profileComboBox = new System.Windows.Forms.ComboBox();
            this.progressBar1 = new Harr.HarrProgressBar();
            this.glowRenderer = new MineClient.GlowRenderer();
            this.SuspendLayout();
            // 
            // startBtn
            // 
            this.startBtn.Location = new System.Drawing.Point(139, 33);
            this.startBtn.Name = "startBtn";
            this.startBtn.Size = new System.Drawing.Size(75, 23);
            this.startBtn.TabIndex = 1;
            this.startBtn.Text = "Start";
            this.startBtn.UseVisualStyleBackColor = true;
            this.startBtn.Visible = false;
            this.startBtn.Click += new System.EventHandler(this.buttonStart_Click);
            // 
            // usernameInput
            // 
            this.usernameInput.Location = new System.Drawing.Point(126, 7);
            this.usernameInput.Name = "usernameInput";
            this.usernameInput.Size = new System.Drawing.Size(100, 20);
            this.usernameInput.TabIndex = 2;
            this.usernameInput.Text = "Player";
            this.usernameInput.Visible = false;
            this.usernameInput.TextChanged += new System.EventHandler(this.usernameInput_TextChanged);
            // 
            // uninstallBtn
            // 
            this.uninstallBtn.Location = new System.Drawing.Point(279, 66);
            this.uninstallBtn.Name = "uninstallBtn";
            this.uninstallBtn.Size = new System.Drawing.Size(61, 23);
            this.uninstallBtn.TabIndex = 3;
            this.uninstallBtn.Text = "Uninstall";
            this.uninstallBtn.UseVisualStyleBackColor = true;
            this.uninstallBtn.Visible = false;
            this.uninstallBtn.Click += new System.EventHandler(this.button1_Click);
            // 
            // profileComboBox
            // 
            this.profileComboBox.FormattingEnabled = true;
            this.profileComboBox.Items.AddRange(new object[] {
            "Loading profiles..."});
            this.profileComboBox.Location = new System.Drawing.Point(13, 66);
            this.profileComboBox.Name = "profileComboBox";
            this.profileComboBox.Size = new System.Drawing.Size(201, 21);
            this.profileComboBox.TabIndex = 4;
            // 
            // progressBar1
            // 
            this.progressBar1.AllowDrag = true;
            this.progressBar1.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right)));
            this.progressBar1.FillDegree = 20;
            this.progressBar1.LeftBarSize = 30;
            this.progressBar1.LeftText = "1";
            this.progressBar1.Location = new System.Drawing.Point(14, 14);
            this.progressBar1.MainText = "Loading";
            this.progressBar1.Margin = new System.Windows.Forms.Padding(5);
            this.progressBar1.Name = "progressBar1";
            this.progressBar1.RightBarSize = 30;
            this.progressBar1.RightText = "7";
            this.progressBar1.RoundedCornerAngle = 10;
            this.progressBar1.Size = new System.Drawing.Size(325, 41);
            this.progressBar1.StatusBarColor = 0;
            this.progressBar1.StatusBarSize = 65;
            this.progressBar1.StatusText = "0";
            this.progressBar1.TabIndex = 0;
            this.progressBar1.Visible = false;
            // 
            // glowRenderer
            // 
            this.glowRenderer.BackColor = System.Drawing.Color.Transparent;
            this.glowRenderer.Location = new System.Drawing.Point(0, 0);
            this.glowRenderer.Name = "glowRenderer";
            this.glowRenderer.Size = new System.Drawing.Size(1, 1);
            this.glowRenderer.TabIndex = 0;
            // 
            // Form1
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.Color.Black;
            this.ClientSize = new System.Drawing.Size(352, 99);
            this.Controls.Add(this.profileComboBox);
            this.Controls.Add(this.uninstallBtn);
            this.Controls.Add(this.startBtn);
            this.Controls.Add(this.progressBar1);
            this.Controls.Add(this.usernameInput);
            this.Controls.Add(this.glowRenderer);
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.MaximizeBox = false;
            this.Name = "Form1";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "MineClient";
            this.Load += new System.EventHandler(this.Form1_Load);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private Button startBtn;
        private Harr.HarrProgressBar progressBar1;
        private TextBox usernameInput;
        private GlowRenderer glowRenderer;
        private Button uninstallBtn;
        private ComboBox profileComboBox;
    }
}

