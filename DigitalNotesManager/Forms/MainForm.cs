using System;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Windows.Forms;
using DigitalNotesManager.Data;
using DigitalNotesManager.Models;
using DigitalNotesManager.Security;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic; // Used for List<Note>
using Timer = System.Windows.Forms.Timer;

namespace DigitalNotesManager
{
    public class MainForm : Form
    {
        // ====================================================================
        // 1. Private Members (Controls & Context)
        // ====================================================================
        private DataGridView dgvNotes;
        private MenuStrip menuStrip;
        private Panel searchPanel;
        private TextBox txtSearch;
        private ComboBox cmbCategory;
        private Button btnSearch;
        private Button btnClear;
        private Button btnDelete;
        private Timer reminderTimer;

        private readonly NotesDbContext _context = new();

        // 🆕 ADDITION: MDI Mode toggle variable
        private bool _mdiMode = true;

        // ====================================================================
        // 2. Constructor & UI Setup
        // ====================================================================
        public MainForm()
        {
            InitializeMainForm();
            CreateUIComponents();
            LoadNotesFromDatabase();
            InitializeReminderTimer();
        }

        private void CreateUIComponents()
        {
            CreateMenu();
            CreateToolStrip();
            CreateSearchPanel();
            CreateNotesGrid();
            dgvNotes.BringToFront();
        }

        // --------------------------------------------------------------------
        // 2.1 Init: Main Form Settings (MDI Integration)
        // --------------------------------------------------------------------
        private void InitializeMainForm()
        {
            Text = "Digital Notes Manager (EF) 📝";
            Width = 1200;
            Height = 800;
            StartPosition = FormStartPosition.CenterScreen;
            // Original Color
            BackColor = ColorTranslator.FromHtml("#F5F6FA");
            Font = new Font("Segoe UI", 10, FontStyle.Regular);
            // 🆕 MDI INTEGRATION
            IsMdiContainer = _mdiMode;
        }

        // --------------------------------------------------------------------
        // 2.2 Init: MenuStrip (MDI & Full Account/Edit Integration)
        // --------------------------------------------------------------------
        private void CreateMenu()
        {
            menuStrip = new MenuStrip
            {
                Font = new Font("Segoe UI", 10),
                BackColor = ColorTranslator.FromHtml("#E3ECFC"),
                ForeColor = ColorTranslator.FromHtml("#1E3C72")
            };

            // FILE Menu
            var file = new ToolStripMenuItem("File");
            file.DropDownItems.Add(new ToolStripMenuItem("📝 New Note", null, NewNote_Click));
            file.DropDownItems.Add(new ToolStripMenuItem("📂 Import (.txt / .json)", null, Import_Click));
            file.DropDownItems.Add(new ToolStripMenuItem("💾 Export Selected (TXT)", null, ExportTxt_Click));
            file.DropDownItems.Add(new ToolStripMenuItem("🖹 Export Selected (RTF)", null, ExportRtf_Click));
            file.DropDownItems.Add(new ToolStripMenuItem("🌐 Export Selected (HTML)", null, ExportHtml_Click));
            file.DropDownItems.Add(new ToolStripSeparator());
            file.DropDownItems.Add(new ToolStripMenuItem("❌ Exit", null, (s, e) => Close()));

            // EDIT Menu (Format Text added)
            var edit = new ToolStripMenuItem("Edit");
            edit.DropDownItems.Add(new ToolStripMenuItem("✂️ Cut", null, (s, e) => SendKeys.Send("^x")));
            edit.DropDownItems.Add(new ToolStripMenuItem("📋 Copy", null, (s, e) => SendKeys.Send("^c")));
            edit.DropDownItems.Add(new ToolStripMenuItem("📥 Paste", null, (s, e) => SendKeys.Send("^v")));
            edit.DropDownItems.Add(new ToolStripMenuItem("🎨 Format Text", null, Format_Click));

            // VIEW Menu (MDI Mode Toggle & Arrange Windows)
            var view = new ToolStripMenuItem("View");
            view.DropDownItems.Add(new ToolStripMenuItem("📑 Refresh Notes List", null, (s, e) => RefreshGrid()));
            view.DropDownItems.Add(new ToolStripMenuItem("🔔 Reminder Center", null, OpenReminders_Click));
            view.DropDownItems.Add(new ToolStripSeparator());

            // 🆕 MDI Toggle Logic
            var mdiToggle = new ToolStripMenuItem("MDI Mode") { Checked = _mdiMode, CheckOnClick = true };
            mdiToggle.CheckedChanged += (s, e) =>
            {
                _mdiMode = mdiToggle.Checked;
                IsMdiContainer = _mdiMode;
                // Close all children when switching out of MDI mode
                if (!_mdiMode)
                {
                    foreach (Form child in MdiChildren) child.Close();
                }
            };
            view.DropDownItems.Add(mdiToggle);

            // 🆕 Arrange Windows Logic
            var arrangeMenu = new ToolStripMenuItem("Arrange Windows");
            arrangeMenu.DropDownItems.Add(new ToolStripMenuItem("Tile Horizontal", null, (s, e) => { if (_mdiMode) LayoutMdi(MdiLayout.TileHorizontal); }));
            arrangeMenu.DropDownItems.Add(new ToolStripMenuItem("Tile Vertical", null, (s, e) => { if (_mdiMode) LayoutMdi(MdiLayout.TileVertical); }));
            arrangeMenu.DropDownItems.Add(new ToolStripMenuItem("Cascade", null, (s, e) => { if (_mdiMode) LayoutMdi(MdiLayout.Cascade); }));
            view.DropDownItems.Add(arrangeMenu);

            // ACCOUNT Menu
            var account = new ToolStripMenuItem("Account");
            account.DropDownItems.Add(new ToolStripMenuItem("⚙ Settings", null, (s, e) => { using var f = new Forms.SettingsForm(); if (f.ShowDialog(this) == DialogResult.OK) { /* settings saved */ } }));
            account.DropDownItems.Add(new ToolStripMenuItem("🔒 Change Password", null, ChangePassword_Click));
            account.DropDownItems.Add(new ToolStripSeparator());
            account.DropDownItems.Add(new ToolStripMenuItem("🚪 Logout", null, Logout_Click));

            // HELP Menu
            var help = new ToolStripMenuItem("Help");
            help.DropDownItems.Add(new ToolStripMenuItem("ℹ️ About", null, ShowAboutDialog));

            menuStrip.Items.AddRange(new ToolStripItem[] { file, edit, view, account, help });
            MainMenuStrip = menuStrip;
            Controls.Add(menuStrip);
        }

        // --------------------------------------------------------------------
        // 2.3 Init: ToolStrip
        // --------------------------------------------------------------------
        private void CreateToolStrip()
        {
            var toolStrip = new ToolStrip
            {
                Dock = DockStyle.Top,
                GripStyle = ToolStripGripStyle.Hidden,
                BackColor = ColorTranslator.FromHtml("#EEF3FF"),
                Font = new Font("Segoe UI", 10)
            };

            // Toolstrip Buttons (Integrated all handlers)
            toolStrip.Items.Add(CreateToolButton("📝", "New Note", NewNote_Click));
            toolStrip.Items.Add(CreateToolButton("📂", "Import", Import_Click));
            toolStrip.Items.Add(CreateToolButton("💾", "Export", SaveFile_Click)); // Reusing SaveFile_Click for general export
            toolStrip.Items.Add(new ToolStripSeparator());
            toolStrip.Items.Add(CreateToolButton("🔍", "Search", (s, e) => txtSearch?.Focus()));
            toolStrip.Items.Add(CreateToolButton("🗑", "Delete", BtnDelete_Click));
            toolStrip.Items.Add(new ToolStripSeparator());
            toolStrip.Items.Add(CreateToolButton("❓", "About", ShowAboutDialog));

            Controls.Add(toolStrip);
        }

        private ToolStripButton CreateToolButton(string icon, string tooltip, EventHandler onClick)
        {
            var btn = new ToolStripButton(icon)
            {
                ToolTipText = tooltip,
                DisplayStyle = ToolStripItemDisplayStyle.Text,
                Font = new Font("Segoe UI Emoji", 12)
            };
            btn.Click += onClick;
            return btn;
        }

        // --------------------------------------------------------------------
        // 2.4 Init: Search Panel
        // --------------------------------------------------------------------
        private void CreateSearchPanel()
        {
            searchPanel = new Panel
            {
                Dock = DockStyle.Top,
                Height = 70,
                Padding = new Padding(15),
                BackColor = ColorTranslator.FromHtml("#EAF0FC")
            };

            var lblSearch = new Label { Text = "Search:", AutoSize = true, Left = 15, Top = 22, ForeColor = ColorTranslator.FromHtml("#1E3C72") };
            txtSearch = new TextBox { Left = 80, Top = 18, Width = 200, PlaceholderText = "Title or Content..." };

            var lblCategory = new Label { Text = "Category:", AutoSize = true, Left = 300, Top = 22, ForeColor = ColorTranslator.FromHtml("#1E3C72") };
            cmbCategory = new ComboBox
            {
                Left = 380,
                Height = 70,
                Top = 18,
                Width = 150,
                DropDownStyle = ComboBoxStyle.DropDownList
            };
            cmbCategory.Items.Add("All");
            cmbCategory.Items.AddRange(new[] { "General", "Personal", "Work", "Study", "Ideas" });
            cmbCategory.SelectedIndex = 0;

            btnSearch = CreateStyledButton("🔍 Search", "#4A90E2", "#2C6BB2");
            btnClear = CreateStyledButton("🧹 Clear", "#F0AD4E", "#D4882F");
            btnDelete = CreateStyledButton("🗑 Delete", "#E57373", "#D32F2F");

            int buttonTop = 15;
            btnSearch.Left = 550; btnSearch.Top = buttonTop;
            btnClear.Left = 670; btnClear.Top = buttonTop;
            btnDelete.Left = 790; btnDelete.Top = buttonTop;

            btnSearch.Click += BtnSearch_Click;
            btnClear.Click += BtnClear_Click;
            btnDelete.Click += BtnDelete_Click;

            searchPanel.Controls.AddRange(new Control[] { lblSearch, txtSearch, lblCategory, cmbCategory, btnSearch, btnClear, btnDelete });
            Controls.Add(searchPanel);
        }

        private Button CreateStyledButton(string text, string normalColor, string hoverColor)
        {
            var btn = new Button
            {
                Text = text,
                Width = 100,
                Height = 30,
                FlatStyle = FlatStyle.Flat,
                BackColor = ColorTranslator.FromHtml(normalColor),
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 11, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btn.FlatAppearance.BorderSize = 0;
            btn.MouseEnter += (s, e) => btn.BackColor = ColorTranslator.FromHtml(hoverColor);
            btn.MouseLeave += (s, e) => btn.BackColor = ColorTranslator.FromHtml(normalColor);
            return btn;
        }

        // --------------------------------------------------------------------
        // 2.5 Init: Notes Grid
        // --------------------------------------------------------------------
        private void CreateNotesGrid()
        {
            dgvNotes = new DataGridView
            {
                Dock = DockStyle.Fill,
                AutoGenerateColumns = true,
                ReadOnly = true,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                MultiSelect = false,
                AllowUserToAddRows = false,
                BackgroundColor = Color.White,
                BorderStyle = BorderStyle.FixedSingle,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                RowTemplate = { Height = 35 }
            };

            dgvNotes.DefaultCellStyle.BackColor = Color.White;
            dgvNotes.DefaultCellStyle.ForeColor = Color.Black;
            dgvNotes.DefaultCellStyle.SelectionBackColor = ColorTranslator.FromHtml("#D2E1FA");
            dgvNotes.DefaultCellStyle.SelectionForeColor = Color.Black;

            dgvNotes.ColumnHeadersDefaultCellStyle.BackColor = ColorTranslator.FromHtml("#E3ECFC");
            dgvNotes.ColumnHeadersDefaultCellStyle.ForeColor = ColorTranslator.FromHtml("#1E3C72");
            dgvNotes.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI", 10, FontStyle.Bold);
            dgvNotes.EnableHeadersVisualStyles = false;

            dgvNotes.CellDoubleClick += DgvNotes_CellDoubleClick;
            Controls.Add(dgvNotes);
        }

        // ====================================================================
        // 3. Data & Core Logic
        // ====================================================================

        private async void LoadNotesFromDatabase()
        {
            if (!AppSession.IsAuthenticated) return;
            var userId = AppSession.CurrentUser!.UserID;
            var notes = await _context.Notes.AsNoTracking().Where(n => n.UserID == userId).ToListAsync();
            RefreshGrid(notes);
        }

        private void RefreshGrid(List<Note>? list = null)
        {
            if (!AppSession.IsAuthenticated)
            {
                dgvNotes.DataSource = null;
                return;
            }

            var userId = AppSession.CurrentUser!.UserID;

            var finalList = list ?? _context.Notes.AsNoTracking().Where(n => n.UserID == userId).ToList();

            var data = finalList.Select(n => new
            {
                n.NoteID,
                n.Title,
                n.Category,
                Created = n.CreationDate.ToString("yyyy-MM-dd HH:mm"),
                Reminder = n.ReminderDate?.ToString("yyyy-MM-dd HH:mm") ?? "N/A"
            }).ToList();

            dgvNotes.DataSource = null;
            dgvNotes.DataSource = data;

            if (dgvNotes.Columns.Contains("NoteID")) dgvNotes.Columns["NoteID"].Visible = false;
            if (dgvNotes.Columns.Contains("Created")) dgvNotes.Columns["Created"].HeaderText = "Creation Date";
            if (dgvNotes.Columns.Contains("Reminder")) dgvNotes.Columns["Reminder"].HeaderText = "Reminder Time";
            if (dgvNotes.Columns.Contains("Title")) dgvNotes.Columns["Title"].HeaderText = "Note Title";
        }

        // ====================================================================
        // 4. Event Handlers (CRUD, File, Account - MDI/Modal Toggle)
        // ====================================================================

        private void NewNote_Click(object? sender, EventArgs e)
        {
            if (!AppSession.IsAuthenticated) return;
            var noteForm = new DigitalNotesManager.Forms.NoteForm();
            noteForm.NoteSaved += _ => RefreshGrid();

            if (_mdiMode)
            {
                noteForm.MdiParent = this;
                noteForm.Show();
            }
            else
            {
                using (noteForm) noteForm.ShowDialog(this);
            }
        }

        private async void DgvNotes_CellDoubleClick(object? sender, DataGridViewCellEventArgs e)
        {
            if (!AppSession.IsAuthenticated || e.RowIndex < 0) return;
            var id = GetSelectedNoteId();
            if (id == null) return;

            var note = await _context.Notes.FindAsync(id.Value);
            if (note == null) return;

            var noteForm = new DigitalNotesManager.Forms.NoteForm(note);
            noteForm.NoteSaved += _ => RefreshGrid();

            if (_mdiMode)
            {
                noteForm.MdiParent = this;
                noteForm.Show();
            }
            else
            {
                using (noteForm) noteForm.ShowDialog(this);
            }
        }

        private async void BtnDelete_Click(object? sender, EventArgs e)
        {
            if (!AppSession.IsAuthenticated) return;
            var id = GetSelectedNoteId();
            if (id == null) { MessageBox.Show("Select a note to delete.", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning); return; }
            var note = await _context.Notes.FindAsync(id.Value); if (note == null) return;
            if (MessageBox.Show($"Delete note: '{note.Title}'?", "Confirm Deletion", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
            {
                _context.Notes.Remove(note);
                await _context.SaveChangesAsync();
                RefreshGrid();
            }
        }

        private void BtnSearch_Click(object? sender, EventArgs e)
        {
            if (!AppSession.IsAuthenticated) return;
            var userId = AppSession.CurrentUser!.UserID;
            string keyword = (txtSearch.Text ?? string.Empty).Trim().ToLower();
            string category = cmbCategory.SelectedItem?.ToString() ?? "All";

            var filtered = _context.Notes.AsNoTracking()
                .Where(n => n.UserID == userId)
                .AsEnumerable()
                .Where(n => (category == "All" || n.Category == category) &&
                            ((n.Title ?? string.Empty).ToLower().Contains(keyword) || (n.Content ?? string.Empty).ToLower().Contains(keyword)))
                .ToList();

            RefreshGrid(filtered);
        }

        private void BtnClear_Click(object? sender, EventArgs e)
        {
            txtSearch.Clear();
            cmbCategory.SelectedIndex = 0;
            RefreshGrid();
        }

        // --- File Export/Import Handlers ---
        private async void OpenFile_Click(object? sender, EventArgs e) // Reusing OpenFile_Click for Import
        {
            if (!AppSession.IsAuthenticated) return;

            using var ofd = new OpenFileDialog
            {
                Filter = "Text Files (*.txt)|*.txt|All Files (*.*)|*.*",
                Title = "Open Note (Import)"
            };
            if (ofd.ShowDialog() != DialogResult.OK) return;

            // Simple TXT import logic remains
            string content = File.ReadAllText(ofd.FileName);
            var note = new Note
            {
                Title = Path.GetFileNameWithoutExtension(ofd.FileName),
                Content = content,
                Category = "Imported",
                CreationDate = DateTime.Now,
                UserID = AppSession.CurrentUser!.UserID
            };

            _context.Notes.Add(note);
            await _context.SaveChangesAsync();
            RefreshGrid();
            MessageBox.Show("Note imported successfully ✅", "Open", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private async void SaveFile_Click(object? sender, EventArgs e) // Reusing SaveFile_Click for Export
        {
            if (!AppSession.IsAuthenticated) return;
            var id = GetSelectedNoteId();
            if (id == null) { MessageBox.Show("Select a note first to export.", "Save", MessageBoxButtons.OK, MessageBoxIcon.Information); return; }

            var note = await _context.Notes.FindAsync(id.Value);
            if (note == null) { MessageBox.Show("Note not found.", "Save", MessageBoxButtons.OK, MessageBoxIcon.Warning); return; }

            // Logic to choose between TXT and JSON export based on extension, but simplified here.
            using var sfd = new SaveFileDialog
            {
                Filter = "Text File (*.txt)|*.txt|JSON (*.json)|*.json",
                FileName = $"{note.Title}".Replace(':', '-').Replace('/', '-')
            };
            if (sfd.ShowDialog() != DialogResult.OK) return;

            if (Path.GetExtension(sfd.FileName).Equals(".json", StringComparison.OrdinalIgnoreCase))
            {
                var json = JsonSerializer.Serialize(note, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(sfd.FileName, json);
            }
            else
            {
                File.WriteAllText(sfd.FileName, $"{note.Title}{Environment.NewLine}{Environment.NewLine}{note.Content}");
            }

            MessageBox.Show("File saved successfully ✅", "Save", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        // --- Full Import Logic (Combined JSON/TXT) ---
        private async void Import_Click(object? s, EventArgs e) // Handler for Menu/ToolStrip Import
        {
            if (!AppSession.IsAuthenticated) return;
            using var ofd = new OpenFileDialog { Filter = "Text/Json|*.txt;*.json|All|*.*", Title = "Import Note" };
            if (ofd.ShowDialog() != DialogResult.OK) return;

            var userId = AppSession.CurrentUser!.UserID;

            if (Path.GetExtension(ofd.FileName).Equals(".json", StringComparison.OrdinalIgnoreCase))
            {
                try
                {
                    var json = File.ReadAllText(ofd.FileName);
                    var note = JsonSerializer.Deserialize<Note>(json);
                    if (note != null)
                    {
                        note.NoteID = 0;
                        note.UserID = userId;
                        note.CreationDate = DateTime.Now;
                        _context.Notes.Add(note);
                        await _context.SaveChangesAsync();
                        RefreshGrid();
                        MessageBox.Show("JSON imported successfully ✅");
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error importing JSON: {ex.Message}", "Import Failed", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
            else
            {
                var content = File.ReadAllText(ofd.FileName, Encoding.UTF8);
                var n = new Note { Title = Path.GetFileNameWithoutExtension(ofd.FileName), Content = content, ContentRtf = null, Category = "Imported", CreationDate = DateTime.Now, UserID = userId };
                _context.Notes.Add(n);
                await _context.SaveChangesAsync();
                RefreshGrid();
                MessageBox.Show("TXT imported successfully ✅");
            }
        }

        // --- Specific Export Handlers (from previous complex logic) ---
        private async void ExportTxt_Click(object? s, EventArgs e)
        {
            var id = GetSelectedNoteId(); if (id == null) { MessageBox.Show("Select a note first."); return; }
            var note = await _context.Notes.FindAsync(id.Value); if (note == null) return;
            using var sfd = new SaveFileDialog { Filter = "Text (*.txt)|*.txt", FileName = $"{note.Title}.txt" };
            if (sfd.ShowDialog() != DialogResult.OK) return;
            File.WriteAllText(sfd.FileName, $"{note.Title}{Environment.NewLine}{Environment.NewLine}{note.Content}", Encoding.UTF8);
            MessageBox.Show("Exported TXT successfully ✅");
        }
        private void ShowAboutDialog(object? sender, EventArgs e)
        {
            MessageBox.Show("📘 Digital Notes Manager\nVersion 1.0.0\nBy: Hossam Mostafa",
                            "About", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private async void ExportRtf_Click(object? s, EventArgs e)
        {
            var id = GetSelectedNoteId(); if (id == null) { MessageBox.Show("Select a note first."); return; }
            var note = await _context.Notes.FindAsync(id.Value); if (note == null) return;
            using var sfd = new SaveFileDialog { Filter = "Rich Text (*.rtf)|*.rtf", FileName = $"{note.Title}.rtf" };
            if (sfd.ShowDialog() != DialogResult.OK) return;

            // Logic: Use existing RTF or generate from plain text content
            var rtf = string.IsNullOrWhiteSpace(note.ContentRtf) ? new RichTextBox { Text = note.Content }.Rtf : note.ContentRtf!;
            File.WriteAllText(sfd.FileName, rtf, Encoding.UTF8);
            MessageBox.Show("Exported RTF successfully ✅");
        }

        private async void ExportHtml_Click(object? s, EventArgs e)
        {
            var id = GetSelectedNoteId(); if (id == null) { MessageBox.Show("Select a note first."); return; }
            var note = await _context.Notes.FindAsync(id.Value); if (note == null) return;
            using var sfd = new SaveFileDialog { Filter = "HTML (*.html)|*.html", FileName = $"{note.Title}.html" };
            if (sfd.ShowDialog() != DialogResult.OK) return;

            // Logic: Minimal HTML wrapper (plain text export)
            var body = System.Net.WebUtility.HtmlEncode(note.Content).Replace("\n", "<br/>");
            var html = $"<html><head><meta charset='utf-8'><title>{System.Net.WebUtility.HtmlEncode(note.Title)}</title></head><body><h2>{System.Net.WebUtility.HtmlEncode(note.Title)}</h2><p>{body}</p></body></html>";

            File.WriteAllText(sfd.FileName, html, Encoding.UTF8);
            MessageBox.Show("Exported HTML successfully ✅ (plain text)");
        }

        // --- Edit Operations (Format) ---
        private void Format_Click(object? sender, EventArgs e)
        {
            var rtb = GetActiveRichTextBox();
            if (rtb == null)
            {
                MessageBox.Show("Open a note editor window first to format text.", "Format", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            using var fontDlg = new FontDialog { ShowColor = true, Font = rtb.SelectionFont ?? rtb.Font, Color = rtb.SelectionColor };
            if (fontDlg.ShowDialog() == DialogResult.OK)
            {
                if (rtb.SelectionLength > 0)
                {
                    rtb.SelectionFont = fontDlg.Font;
                    rtb.SelectionColor = fontDlg.Color;
                }
                else
                {
                    rtb.Font = fontDlg.Font;
                    rtb.ForeColor = fontDlg.Color;
                }
            }
        }

        // --- Account Handlers ---
        private void Logout_Click(object? sender, EventArgs e)
        {
            AppSession.Logout();
            RefreshGrid();
            using var login = new DigitalNotesManager.Forms.LoginForm();
            var res = login.ShowDialog(this);
            if (res == DialogResult.OK && AppSession.IsAuthenticated)
            {
                LoadNotesFromDatabase();
            }
            else
            {
                Close();
            }
        }

        private void ChangePassword_Click(object? sender, EventArgs e)
        {
            if (!AppSession.IsAuthenticated || AppSession.CurrentUser == null)
            {
                MessageBox.Show("Please login first.", "Account", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            using var f = new DigitalNotesManager.Forms.ChangePasswordForm(AppSession.CurrentUser.UserID);
            f.ShowDialog(this);
        }

        // ====================================================================
        // 5. Reminders Logic
        // ====================================================================

        private void InitializeReminderTimer()
        {
            reminderTimer = new Timer { Interval = 60000 };
            reminderTimer.Tick += ReminderTimer_Tick;
            reminderTimer.Start();
        }

        private void ReminderTimer_Tick(object? sender, EventArgs e)
        {
            if (!AppSession.IsAuthenticated) return;
            var userId = AppSession.CurrentUser!.UserID;
            var now = DateTime.Now;

            var dueNotes = _context.Notes
                .Where(n => n.UserID == userId && n.ReminderDate.HasValue && n.ReminderDate <= now && !n.IsReminderShown)
                .ToList();

            foreach (var note in dueNotes)
            {
                note.IsReminderShown = true;
                MessageBox.Show($"🔔 Reminder Due:\n\nTitle: {note.Title}\nTime: {note.ReminderDate?.ToString("yyyy-MM-dd HH:mm")}", "Note Reminder", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }

            if (dueNotes.Count > 0) _context.SaveChanges();
        }

        private void OpenReminders_Click(object? sender, EventArgs e)
        {
            if (!AppSession.IsAuthenticated) return;
            using var reminderCenter = new DigitalNotesManager.Forms.ReminderCenterForm();
            reminderCenter.ShowDialog(this);
        }

        // ====================================================================
        // 6. Helpers (MDI/RichTextBox Logic)
        // ====================================================================

        private int? GetSelectedNoteId()
        {
            if (dgvNotes == null || dgvNotes.CurrentRow == null || !dgvNotes.Columns.Contains("NoteID"))
                return null;
            var val = dgvNotes.CurrentRow.Cells["NoteID"].Value?.ToString();
            return int.TryParse(val, out var id) ? id : (int?)null;
        }

        // 🆕 GetActiveRichTextBox updated for MDI/Modal compatibility
        private RichTextBox? GetActiveRichTextBox()
        {
            if (_mdiMode && ActiveMdiChild is DigitalNotesManager.Forms.NoteForm mf)
            {
                // If MDI is enabled and a NoteForm is active, try finding RTB in that child's controls
                var r = FindChildRichTextBox(mf.Controls);
                if (r != null) return r;
            }

            // Fallback for Modal windows or when MDI is disabled
            foreach (Form f in Application.OpenForms)
            {
                if (f is DigitalNotesManager.Forms.NoteForm nf && nf.ContainsFocus)
                {
                    var r = FindChildRichTextBox(nf.Controls);
                    if (r != null) return r;
                }
            }
            return null;
        }

        // 🆕 FindChildRichTextBox (Recursive logic retained)
        private RichTextBox? FindChildRichTextBox(Control.ControlCollection controls)
        {
            foreach (Control c in controls)
            {
                if (c is RichTextBox r) return r;
                // Recursive call for nested controls
                var nested = FindChildRichTextBox(c.Controls);
                if (nested != null) return nested;
            }
            return null;
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _context?.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}