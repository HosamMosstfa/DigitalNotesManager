using DigitalNotesManager.Security;
using DigitalNotesManager.Services;
using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace DigitalNotesManager.Forms
{
    public class SettingsForm : Form
    {
        // ====================================================================
        // 1. Private Members (Controls & Data)
        // ====================================================================
        private TextBox txtFontFamily;
        private NumericUpDown numFontSize;
        private CheckBox chkDefaultReminder;
        private TextBox txtCategories;
        private Button btnSave;
        private Button btnCancel;
        private AppSettings _settings;

        // --- Labels (Declared here for clarity, initialized in methods) ---
        private Label lblFontFamily;
        private Label lblFontSize;
        private Label lblCats;

        // ====================================================================
        // 2. Constructor & Initialization
        // ====================================================================
        public SettingsForm()
        {
            var username = AppSession.CurrentUser?.Username ?? "Default";
            _settings = AppSettings.Load(username);

            InitializeUI();

            // Populate data after UI is initialized
            PopulateControls();
        }

        private void InitializeUI()
        {
            // --- Form Settings (UX) ---
            Text = "⚙️ Application Settings";
            Width = 550;
            Height = 480; // زيادة الارتفاع لاستيعاب التجميعات
            StartPosition = FormStartPosition.CenterParent;
            Font = new Font("Segoe UI", 10);
            BackColor = ColorTranslator.FromHtml("#F5F7FB"); // لون خلفية فاتح
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;

            int margin = 20;
            int controlX = 180;
            int controlWidth = 330;
            int yPos = margin;

            // --- Group 1: Editor Settings ---
            var editorGroup = CreateGroupBox("Editor Defaults", margin, yPos, ClientSize.Width - 2 * margin, 180);

            // 1. Font Family
            lblFontFamily = CreateLabel("Font Family:", 15, 30);
            txtFontFamily = CreateTextBox(180, 26, controlWidth - 25, _settings.EditorFontFamily);
            editorGroup.Controls.Add(lblFontFamily);
            editorGroup.Controls.Add(txtFontFamily);

            // 2. Font Size
            lblFontSize = CreateLabel("Font Size:", 15, 70);
            numFontSize = CreateNumericUpDown(190, 66, 100, (decimal)_settings.EditorFontSize, 6, 48);
            editorGroup.Controls.Add(lblFontSize);
            editorGroup.Controls.Add(numFontSize);

            // 3. Default Reminder Checkbox
            chkDefaultReminder = new CheckBox
            {
                Left = 15,
                Top = 110,
                Width = 280,
                Text = "New notes start with Reminder checked",
                Checked = _settings.DefaultReminderChecked
            };
            editorGroup.Controls.Add(chkDefaultReminder);

            yPos += editorGroup.Height + margin;

            // --- Group 2: Categories ---
            var categoryGroup = CreateGroupBox("Default Categories", margin, yPos, ClientSize.Width - 2 * margin, 160);

            lblCats = CreateLabel("List (comma or newline separated):", 15, 30);
            lblCats.Width = 300;
            lblCats.TextAlign = ContentAlignment.TopLeft;

            txtCategories = new TextBox
            {
                Left = 15,
                Top = 55,
                Width = categoryGroup.Width - 30,
                Height = 80,
                Multiline = true,
                ScrollBars = ScrollBars.Vertical
            };
            categoryGroup.Controls.Add(lblCats);
            categoryGroup.Controls.Add(txtCategories);

            yPos += categoryGroup.Height + 10;

            // --- Buttons ---
            btnSave = CreateStyledButton("💾 Save Settings", "#4A90E2", "#2C6CD4");
            btnSave.Left = ClientSize.Width - 300;
            btnSave.Top = yPos;
            btnSave.Click += BtnSave_Click;

            btnCancel = CreateStyledButton("✖ Cancel", "#78909C", "#546E7A"); // لون محايد للإلغاء
            btnCancel.Left = ClientSize.Width - 150;
            btnCancel.Top = yPos;
            btnCancel.Click += (s, e) => { DialogResult = DialogResult.Cancel; Close(); };

            // Accept/Cancel buttons
            AcceptButton = btnSave;
            CancelButton = btnCancel;

            Controls.AddRange(new Control[] { editorGroup, categoryGroup, btnSave, btnCancel });
        }

        // Helper to load settings data into the controls
        private void PopulateControls()
        {
            txtCategories.Text = string.Join(Environment.NewLine, _settings.DefaultCategories);
            // Ensure numeric values are safe
            numFontSize.Value = Math.Max(numFontSize.Minimum, Math.Min(numFontSize.Maximum, (decimal)_settings.EditorFontSize));
        }


        // ====================================================================
        // 4. Helper Methods (UI Element Creation)
        // ====================================================================

        private GroupBox CreateGroupBox(string text, int left, int top, int width, int height) => new GroupBox
        {
            Text = text,
            Left = left,
            Top = top,
            Width = width,
            Height = height,
            Font = new Font("Segoe UI", 10, FontStyle.Bold),
            ForeColor = ColorTranslator.FromHtml("#1E3C72")
        };

        private Label CreateLabel(string text, int left, int top) => new Label
        {
            Text = text,
            Left = left,
            Top = top + 4,
            Width = 160,
            TextAlign = ContentAlignment.MiddleRight,
            Font = new Font("Segoe UI", 10, FontStyle.Regular),
            ForeColor = ColorTranslator.FromHtml("#333333")
        };

        private TextBox CreateTextBox(int left, int top, int width, string defaultText) => new TextBox
        {
            Left = left,
            Top = top,
            Width = width,
            Height = 26,
            BorderStyle = BorderStyle.FixedSingle,
            Font = new Font("Segoe UI", 10),
            Text = defaultText
        };

        private NumericUpDown CreateNumericUpDown(int left, int top, int width, decimal value, decimal min, decimal max) => new NumericUpDown
        {
            Left = left,
            Top = top,
            Width = width,
            Minimum = min,
            Maximum = max,
            DecimalPlaces = 0,
            Value = value,
            BorderStyle = BorderStyle.FixedSingle
        };

        private Button CreateStyledButton(string text, string normalColor, string hoverColor)
        {
            var btn = new Button
            {
                Text = text,
                Width = 130,
                Height = 40,
                FlatStyle = FlatStyle.Flat,
                BackColor = ColorTranslator.FromHtml(normalColor),
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 10, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btn.FlatAppearance.BorderSize = 0;
            btn.MouseEnter += (s, e) => btn.BackColor = ColorTranslator.FromHtml(hoverColor);
            btn.MouseLeave += (s, e) => btn.BackColor = ColorTranslator.FromHtml(normalColor);
            return btn;
        }

        // ====================================================================
        // 5. Event Handler (Save Logic)
        // ====================================================================
        private void BtnSave_Click(object? sender, EventArgs e)
        {
            // Update settings object with control values
            _settings.EditorFontFamily = txtFontFamily.Text.Trim().Length == 0 ? "Segoe UI" : txtFontFamily.Text.Trim();
            _settings.EditorFontSize = (float)numFontSize.Value;
            _settings.DefaultReminderChecked = chkDefaultReminder.Checked;

            // Clean and process categories input
            _settings.DefaultCategories = txtCategories.Text
                // Split by comma, newline, or forward slash
                .Split(new[] { ',', '\n', '/' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(c => c.Trim())
                .Where(c => c.Length > 0)
                .Distinct()
                .ToList();

            // Save settings to storage
            _settings.Save(AppSession.CurrentUser?.Username ?? "Default");

            // Close the form successfully
            DialogResult = DialogResult.OK;
            Close();
        }
    }
}