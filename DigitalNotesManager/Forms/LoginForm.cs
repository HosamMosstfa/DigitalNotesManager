using System;
using System.Drawing;
using System.Windows.Forms;
using DigitalNotesManager.Data;
using DigitalNotesManager.Security;
using DigitalNotesManager.Services;

namespace DigitalNotesManager.Forms
{
    public class LoginForm : Form
    {
        // Controls
        private Label lblHeader;
        private Label lblUser;
        private Label lblPass;
        private Label lblError;
        private LinkLabel lnkCreate;

        private TextBox txtUsername;
        private TextBox txtPassword;
        private Button btnToggle;

        private Button btnLogin;
        private Button btnCancel;

        public LoginForm()
        {
            InitializeUI();
        }

        private void InitializeUI()
        {
            // Form
            Text = "Login";
            Width = 420;
            Height = 300;
            StartPosition = FormStartPosition.CenterScreen;
            Font = new Font("Segoe UI", 10);
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            BackColor = Color.White;

            // Header
            lblHeader = new Label
            {
                Text = "Sign in to Digital Notes",
                Left = 20,
                Top = 18,
                Width = 360,
                Font = new Font("Segoe UI", 13, FontStyle.Bold),
                ForeColor = ColorTranslator.FromHtml("#1E3C72")
            };

            // Username
            lblUser = new Label
            {
                Text = "Username",
                Left = 20,
                Top = 60,
                Width = 90,
                ForeColor = ColorTranslator.FromHtml("#34495E")
            };

            txtUsername = new TextBox
            {
                Left = 120,
                Top = 56,
                Width = 260,
                BorderStyle = BorderStyle.FixedSingle,
                BackColor = Color.White,
                ForeColor = ColorTranslator.FromHtml("#333"),
                PlaceholderText = "Enter username",
                TabIndex = 0
            };

            // Password + toggle
            lblPass = new Label
            {
                Text = "Password",
                Left = 20,
                Top = 100,
                Width = 90,
                ForeColor = ColorTranslator.FromHtml("#34495E")
            };

            txtPassword = new TextBox
            {
                Left = 120,
                Top = 96,
                Width = 180,
                BorderStyle = BorderStyle.FixedSingle,
                BackColor = Color.White,
                ForeColor = ColorTranslator.FromHtml("#333"),
                PlaceholderText = "Enter password",
                UseSystemPasswordChar = true,
                TabIndex = 1
            };
            lnkCreate = new LinkLabel
            {
                Text = "Create a new account",
                Left = 250,  // ظبط الإحداثيات حسب التصميم عندك
                Top = 130,
                AutoSize = true
            };
            lnkCreate.LinkClicked += (s, e) =>
            {
                using var reg = new RegistrationForm();
                var res = reg.ShowDialog(this);
                if (res == DialogResult.OK && !string.IsNullOrEmpty(reg.RegisteredUsername))
                {
                    txtUsername.Text = reg.RegisteredUsername;
                    txtPassword.Focus();
                }
            };

            Controls.Add(lnkCreate);

            btnToggle = new Button
            {
                Text = "Show",
                Left = txtPassword.Right + 8,
                Top = txtPassword.Top - 1,
                Width = 72,
                Height = txtPassword.Height + 2,
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.Gainsboro,
                ForeColor = Color.Black,
                TabStop = false
            };
            btnToggle.FlatAppearance.BorderSize = 0;
            btnToggle.Click += (s, e) =>
            {
                txtPassword.UseSystemPasswordChar = !txtPassword.UseSystemPasswordChar;
                btnToggle.Text = txtPassword.UseSystemPasswordChar ? "Show" : "Hide";
            };

            // Error (inline)
            lblError = new Label
            {
                Text = string.Empty,
                Left = 20,
                Top = 135,
                Width = 360,
                ForeColor = Color.Firebrick,
                Visible = false
            };

            // Buttons
            btnLogin = CreatePrimaryButton("Login", "#4A90E2", "#2C6CD4");
            btnLogin.Left = 120;
            btnLogin.Top = 170;
            btnLogin.Width = 110;
            btnLogin.Height = 36;
            btnLogin.TabIndex = 2;
            btnLogin.Click += BtnLogin_Click;

            btnCancel = CreatePrimaryButton("Cancel", "#95A5A6", "#7F8C8D");
            btnCancel.Left = btnLogin.Right + 12;
            btnCancel.Top = 170;
            btnCancel.Width = 110;
            btnCancel.Height = 36;
            btnCancel.TabIndex = 3;
            btnCancel.Click += (s, e) => { DialogResult = DialogResult.Cancel; Close(); };

            AcceptButton = btnLogin;
            CancelButton = btnCancel;

            Controls.AddRange(new Control[]
            {
                lblHeader,
                lblUser, txtUsername,
                lblPass, txtPassword, btnToggle,
                lblError,
                btnLogin, btnCancel
            });
        }

        private Button CreatePrimaryButton(string text, string normalHex, string hoverHex)
        {
            var btn = new Button
            {
                Text = text,
                FlatStyle = FlatStyle.Flat,
                BackColor = ColorTranslator.FromHtml(normalHex),
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 10, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btn.FlatAppearance.BorderSize = 0;
            btn.MouseEnter += (s, e) => btn.BackColor = ColorTranslator.FromHtml(hoverHex);
            btn.MouseLeave += (s, e) => btn.BackColor = ColorTranslator.FromHtml(normalHex);
            return btn;
        }

        private void BtnLogin_Click(object? sender, EventArgs e)
        {
            lblError.Visible = false;
            lblError.Text = "";

            var username = txtUsername.Text.Trim();
            var password = txtPassword.Text;

            if (string.IsNullOrWhiteSpace(username) || string.IsNullOrEmpty(password))
            {
                ShowError("Please enter both username and password.");
                return;
            }

            try
            {
                using var ctx = new NotesDbContext();
                var service = new UserService(ctx);

                var user = service.Authenticate(username, password);
                if (user == null)
                {
                    ShowError("Invalid username or password.");
                    return;
                }

                AppSession.Login(user);
                DialogResult = DialogResult.OK;
                Close();
            }
            catch (Exception ex)
            {
                ShowError($"System error: {ex.Message}");
            }
        }

        private void ShowError(string msg)
        {
            lblError.Text = msg;
            lblError.Visible = true;
        }
    }
}
