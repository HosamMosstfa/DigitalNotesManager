using System;
using System.Drawing;
using System.Windows.Forms;
using DigitalNotesManager.Data;
using DigitalNotesManager.Services;
using DigitalNotesManager.Security; // لو معندكش PasswordHasher، سيب السطر ده عادي

namespace DigitalNotesManager.Forms
{
    public class RegistrationForm : Form
    {
        private TextBox txtUsername;
        private TextBox txtPassword;
        private TextBox txtConfirm;
        private CheckBox chkShow;
        private Button btnRegister;
        private Button btnCancel;
        private Label lblUser, lblPass, lblConfirm, lblTitle, lblHint;

        public string? RegisteredUsername { get; private set; }

        public RegistrationForm()
        {
            Text = "Create Account";
            Width = 440;
            Height = 380;
            StartPosition = FormStartPosition.CenterParent;
            BackColor = Color.White;
            Font = new Font("Segoe UI", 10);
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;

            lblTitle = new Label
            {
                Text = "Create a new account",
                Font = new Font("Segoe UI", 12, FontStyle.Bold),
                ForeColor = Color.FromArgb(0x1E, 0x3C, 0x72),
                Left = 20, Top = 18, Width = 360
            };

            lblUser = new Label { Text = "Username:", Left = 20, Top = 70, Width = 100, TextAlign = ContentAlignment.MiddleRight };
            txtUsername = new TextBox { Left = 130, Top = 68, Width = 250, PlaceholderText = "e.g. hmostafa" };

            lblPass = new Label { Text = "Password:", Left = 20, Top = 115, Width = 100, TextAlign = ContentAlignment.MiddleRight };
            txtPassword = new TextBox { Left = 130, Top = 112, Width = 250, UseSystemPasswordChar = true, PlaceholderText = "Min 6 chars" };

            lblConfirm = new Label { Text = "Confirm:", Left = 20, Top = 160, Width = 100, TextAlign = ContentAlignment.MiddleRight };
            txtConfirm = new TextBox { Left = 130, Top = 157, Width = 250, UseSystemPasswordChar = true, PlaceholderText = "Repeat password" };

            chkShow = new CheckBox { Text = "Show password", Left = 130, Top = 195, Width = 250 };
            chkShow.CheckedChanged += (s, e) =>
            {
                bool show = chkShow.Checked;
                txtPassword.UseSystemPasswordChar = !show;
                txtConfirm.UseSystemPasswordChar = !show;
            };

            lblHint = new Label
            {
                Left = 130,
                Top = 225,
                Width = 260,
                Height = 38,
                ForeColor = Color.Gray,
                Text = "• Username must be unique\n• Password ≥ 6 characters"
            };

            btnRegister = CreateButton("Register", 130, 280, "#4A90E2", "#2C6CD4");
            btnRegister.Click += BtnRegister_Click;

            btnCancel = CreateButton("Cancel", 270, 280, "#95A5A6", "#7F8C8D");
            btnCancel.Click += (s, e) => { DialogResult = DialogResult.Cancel; Close(); };

            AcceptButton = btnRegister;
            CancelButton = btnCancel;

            Controls.AddRange(new Control[]
            {
                lblTitle, lblUser, txtUsername, lblPass, txtPassword, lblConfirm, txtConfirm,
                chkShow, lblHint, btnRegister, btnCancel
            });
        }

        private Button CreateButton(string text, int left, int top, string normalHex, string hoverHex)
        {
            var btn = new Button
            {
                Text = text,
                Left = left,
                Top = top,
                Width = 130,
                Height = 38,
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

        private void BtnRegister_Click(object? sender, EventArgs e)
        {
            var username = (txtUsername.Text ?? "").Trim();
            var pass = txtPassword.Text ?? "";
            var confirm = txtConfirm.Text ?? "";

            // Validation
            if (string.IsNullOrWhiteSpace(username))
            {
                MessageBox.Show("Please enter a username.", "Validation", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                txtUsername.Focus(); return;
            }
            if (pass.Length < 6)
            {
                MessageBox.Show("Password must be at least 6 characters.", "Validation", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                txtPassword.Focus(); return;
            }
            if (pass != confirm)
            {
                MessageBox.Show("Password and confirmation do not match.", "Validation", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                txtConfirm.Focus(); return;
            }

            try
            {
                using var ctx = new NotesDbContext();
                var service = new UserService(ctx);

                if (service.UsernameExists(username))
                {
                    MessageBox.Show("This username is already taken. Please choose another.", "Registration", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    txtUsername.Focus(); return;
                }

                // لو عندك PasswordHasher، هيتطبق جوه Register. لو مش موجود، هيخزن Plain (مش مستحب).
                var user = service.Register(username, pass);
                if (user == null)
                {
                    MessageBox.Show("Registration failed. Please try again.", "Registration", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                RegisteredUsername = username;
                MessageBox.Show("Account created successfully ✅", "Registration", MessageBoxButtons.OK, MessageBoxIcon.Information);
                DialogResult = DialogResult.OK;
                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error during registration: {ex.Message}", "Registration", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}
