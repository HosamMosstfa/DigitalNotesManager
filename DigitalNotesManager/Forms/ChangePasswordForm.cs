using System;
using System.Drawing;
using System.Windows.Forms;
using DigitalNotesManager.Data;
using DigitalNotesManager.Services;

namespace DigitalNotesManager.Forms
{
    public class ChangePasswordForm : Form
    {
        private readonly int _userId;
        private readonly NotesDbContext _ctx;
        private readonly UserService _service;

        private Label lblHeader, lblCurrent, lblNew, lblConfirm, lblError;
        private TextBox txtCurrent, txtNew, txtConfirm;
        private Button btnSave, btnCancel;

        public ChangePasswordForm(int userId)
        {
            _userId = userId;
            _ctx = new NotesDbContext();
            _service = new UserService(_ctx);
            InitializeUI();
        }

        private void InitializeUI()
        {
            Text = "Change Password";
            Width = 460;
            Height = 300;
            StartPosition = FormStartPosition.CenterParent;
            Font = new Font("Segoe UI", 10);
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            BackColor = Color.White;

            lblHeader = new Label
            {
                Text = "Update your password",
                Left = 20,
                Top = 16,
                Width = 360,
                Font = new Font("Segoe UI", 13, FontStyle.Bold),
                ForeColor = ColorTranslator.FromHtml("#1E3C72")
            };

            lblCurrent = new Label { Text = "Current", Left = 20, Top = 60, Width = 90, ForeColor = ColorTranslator.FromHtml("#34495E") };
            txtCurrent = new TextBox { Left = 120, Top = 56, Width = 280, BorderStyle = BorderStyle.FixedSingle, UseSystemPasswordChar = true };

            lblNew = new Label { Text = "New", Left = 20, Top = 100, Width = 90, ForeColor = ColorTranslator.FromHtml("#34495E") };
            txtNew = new TextBox { Left = 120, Top = 96, Width = 280, BorderStyle = BorderStyle.FixedSingle, UseSystemPasswordChar = true };

            lblConfirm = new Label { Text = "Confirm", Left = 20, Top = 140, Width = 90, ForeColor = ColorTranslator.FromHtml("#34495E") };
            txtConfirm = new TextBox { Left = 120, Top = 136, Width = 280, BorderStyle = BorderStyle.FixedSingle, UseSystemPasswordChar = true };

            lblError = new Label { Text = "", Left = 20, Top = 170, Width = 380, ForeColor = Color.Firebrick, Visible = false };

            btnSave = CreateBtn("Save", "#4A90E2", "#2C6CD4");
            btnSave.Left = 120; btnSave.Top = 200; btnSave.Width = 110; btnSave.Height = 36;
            btnSave.Click += BtnSave_Click;

            btnCancel = CreateBtn("Cancel", "#95A5A6", "#7F8C8D");
            btnCancel.Left = btnSave.Right + 12; btnCancel.Top = 200; btnCancel.Width = 110; btnCancel.Height = 36;
            btnCancel.Click += (s, e) => Close();

            AcceptButton = btnSave;
            CancelButton = btnCancel;

            Controls.AddRange(new Control[]
            {
                lblHeader,
                lblCurrent, txtCurrent,
                lblNew, txtNew,
                lblConfirm, txtConfirm,
                lblError,
                btnSave, btnCancel
            });
        }

        private Button CreateBtn(string text, string normalHex, string hoverHex)
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

        private void BtnSave_Click(object? sender, EventArgs e)
        {
            lblError.Visible = false;
            lblError.Text = "";

            var cur = txtCurrent.Text;
            var nw = txtNew.Text;
            var cf = txtConfirm.Text;

            if (string.IsNullOrWhiteSpace(cur) || string.IsNullOrWhiteSpace(nw) || string.IsNullOrWhiteSpace(cf))
            {
                ShowError("All fields are required.");
                return;
            }

            if (nw != cf)
            {
                ShowError("New password and confirmation do not match.");
                return;
            }

            if (nw.Length < 6)
            {
                ShowError("New password is too short (min 6).");
                return;
            }

            if (_service.ChangePassword(_userId, cur, nw, out var err))
            {
                MessageBox.Show("Password updated successfully ✅", "Change Password", MessageBoxButtons.OK, MessageBoxIcon.Information);
                Close();
                return;
            }

            ShowError(err);
        }

        private void ShowError(string msg)
        {
            lblError.Text = msg;
            lblError.Visible = true;
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _ctx?.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}
