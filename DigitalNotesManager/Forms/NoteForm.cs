using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using DigitalNotesManager.Models;
using DigitalNotesManager.Data;
using DigitalNotesManager.Controls;
using Microsoft.EntityFrameworkCore;
using DigitalNotesManager.Security;

namespace DigitalNotesManager.Forms
{
    public class NoteForm : Form
    {
        // ========== Controls ==========
        private TextBox txtTitle;
        private CategorySelector categorySelector;
        private RichTextBox rtbContent;
        private DateTimePicker dtpReminder;
        private CheckBox chkHasReminder;
        private Button btnSave, btnCancel;
        private Label lblTitle, lblCategory, lblContent, lblReminder, lblCounter;

        // inline formatting toolbar
        private Panel editorToolbar;
        private Button btnBold, btnItalic, btnUnderline, btnBullet, btnAlignLeft, btnAlignCenter, btnAlignRight;
        private ComboBox cmbFontName, cmbFontSize;
        private Button btnClear, btnCopy;
        private ContextMenuStrip rtbMenu;

        private readonly NotesDbContext _context = new NotesDbContext();
        private Note? currentNote;
        private bool isDirty = false;
        private bool isSaving = false; // <=== التعديل الأول: إضافة هذا المتغير

        public event Action<Note>? NoteSaved;

        public NoteForm()
        {
            InitializeForm();
            _ = LoadCategoriesFromDbAsync();
        }

        public NoteForm(Note note)
        {
            currentNote = note;
            InitializeForm();
            _ = LoadCategoriesFromDbAsync();
            PopulateFromNote();
        }

        private void InitializeForm()
        {
            Text = currentNote == null ? "Add New Note" : $"Edit Note: {currentNote.Title}";
            Width = 780;
            Height = 720;
            StartPosition = FormStartPosition.CenterParent;
            BackColor = Color.White;
            Font = new Font("Segoe UI", 10);
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            KeyPreview = true;
            KeyDown += (s, e) =>
            {
                if (e.Control && e.KeyCode == Keys.S) { BtnSave_Click(s, EventArgs.Empty); e.Handled = true; }
                if (e.KeyCode == Keys.Escape) { Close(); e.Handled = true; }
            };

            int labelX = 20, controlX = 140, controlWidth = 600;

            // Title
            lblTitle = CreateLabel("Title:", labelX, 22);
            txtTitle = CreateTextBox(controlX, 18, controlWidth);
            txtTitle.PlaceholderText = "Enter note title...";
            txtTitle.TextChanged += (_, __) => isDirty = true;

            // Category
            lblCategory = CreateLabel("Category:", labelX, 62);
            categorySelector = new CategorySelector
            {
                Left = controlX,
                Top = 58,
                Width = 320
            };
            categorySelector.CategoryChanged += _ => isDirty = true;

            // Content label
            lblContent = CreateLabel("Content:", labelX, 110);

            // toolbar
            BuildEditorToolbar(controlX, 102, controlWidth);

            // RichTextBox
            rtbContent = new RichTextBox
            {
                Left = controlX,
                Top = editorToolbar.Bottom + 6,
                Width = controlWidth,
                Height = 340,
                BorderStyle = BorderStyle.FixedSingle,
                BackColor = Color.White,
                ForeColor = ColorTranslator.FromHtml("#333"),
                Font = new Font("Segoe UI", 10),
                DetectUrls = true,
                WordWrap = true,
                HideSelection = false
            };
            rtbContent.SelectionChanged += (_, __) => SyncToolbarWithSelection();
            rtbContent.TextChanged += (_, __) => { isDirty = true; UpdateCounter(); };

            // context menu
            rtbMenu = new ContextMenuStrip();
            rtbMenu.Items.Add("Cut", null, (s, e) => rtbContent.Cut());
            rtbMenu.Items.Add("Copy", null, (s, e) => rtbContent.Copy());
            rtbMenu.Items.Add("Paste", null, (s, e) => rtbContent.Paste());
            rtbMenu.Items.Add(new ToolStripSeparator());
            rtbMenu.Items.Add("Select All", null, (s, e) => rtbContent.SelectAll());
            rtbContent.ContextMenuStrip = rtbMenu;

            // counter & quick actions
            lblCounter = new Label
            {
                Left = controlX + 200,
                Top = rtbContent.Bottom + 8,
                Width = 220,
                Height = 20,
                ForeColor = Color.Gray,
                TextAlign = ContentAlignment.MiddleLeft
            };
            UpdateCounter();

            btnClear = CreateUtilityButton("Clear", controlX, rtbContent.Bottom + 4);
            btnClear.Click += (_, __) => rtbContent.Clear();

            btnCopy = CreateUtilityButton("Copy All", controlX + 90, rtbContent.Bottom + 4);
            btnCopy.Click += (_, __) => { if (!string.IsNullOrEmpty(rtbContent.Text)) Clipboard.SetText(rtbContent.Text); };

            // Reminder
            lblReminder = CreateLabel("Reminder:", labelX, lblCounter.Bottom + 20);
            chkHasReminder = new CheckBox { Left = controlX, Top = lblCounter.Bottom + 18, Width = 20, Text = "" };
            dtpReminder = new DateTimePicker
            {
                Left = controlX + 28,
                Top = lblCounter.Bottom + 14,
                Width = 260,
                Format = DateTimePickerFormat.Custom,
                CustomFormat = "yyyy-MM-dd HH:mm",
                Enabled = false
            };
            chkHasReminder.CheckedChanged += (s, e) =>
            {
                dtpReminder.Enabled = chkHasReminder.Checked;
                if (!chkHasReminder.Checked) dtpReminder.Value = DateTime.Now;
                isDirty = true;
            };

            // Buttons
            btnSave = CreateButton("Save", controlX, dtpReminder.Bottom + 18, "#4A90E2", "#2C6CD4");
            btnSave.Click += BtnSave_Click;
            btnCancel = CreateButton("Cancel", controlX + 160, dtpReminder.Bottom + 18, "#E57373", "#D32F2F");
            btnCancel.Click += (_, __) => Close();

            AcceptButton = btnSave;
            CancelButton = btnCancel;

            Controls.AddRange(new Control[]
            {
                lblTitle, txtTitle,
                lblCategory, categorySelector,
                lblContent, editorToolbar, rtbContent,
                btnClear, btnCopy, lblCounter,
                lblReminder, chkHasReminder, dtpReminder,
                btnSave, btnCancel
            });
        }

        private void BuildEditorToolbar(int left, int top, int width)
        {
            editorToolbar = new Panel
            {
                Left = left,
                Top = top,
                Width = width,
                Height = 36,
                BackColor = Color.FromArgb(0xF3, 0xF6, 0xFE)
            };

            btnBold = MakeTbButton("B");
            btnItalic = MakeTbButton("I");
            btnUnderline = MakeTbButton("U");
            btnBullet = MakeTbButton("•");
            btnAlignLeft = MakeTbButton("⟸");
            btnAlignCenter = MakeTbButton("⇔");
            btnAlignRight = MakeTbButton("⟹");

            btnBold.Click += (_, __) => ToggleStyle(FontStyle.Bold);
            btnItalic.Click += (_, __) => ToggleStyle(FontStyle.Italic);
            btnUnderline.Click += (_, __) => ToggleStyle(FontStyle.Underline);
            btnBullet.Click += (_, __) => rtbContent.SelectionBullet = !rtbContent.SelectionBullet;
            btnAlignLeft.Click += (_, __) => rtbContent.SelectionAlignment = HorizontalAlignment.Left;
            btnAlignCenter.Click += (_, __) => rtbContent.SelectionAlignment = HorizontalAlignment.Center;
            btnAlignRight.Click += (_, __) => rtbContent.SelectionAlignment = HorizontalAlignment.Right;

            cmbFontName = new ComboBox
            {
                Left = 8,
                Top = 6,
                Width = 180,
                DropDownStyle = ComboBoxStyle.DropDownList
            };
            foreach (FontFamily ff in FontFamily.Families)
                cmbFontName.Items.Add(ff.Name);
            cmbFontName.SelectedItem = "Segoe UI";
            cmbFontName.SelectedIndexChanged += (_, __) => ApplyFontFromToolbar();

            cmbFontSize = new ComboBox
            {
                Left = cmbFontName.Right + 6,
                Top = 6,
                Width = 70,
                DropDownStyle = ComboBoxStyle.DropDownList
            };
            cmbFontSize.Items.AddRange(new object[] { "9", "10", "11", "12", "14", "16", "18", "20", "22", "24", "28" });
            cmbFontSize.SelectedItem = "10";
            cmbFontSize.SelectedIndexChanged += (_, __) => ApplyFontFromToolbar();

            int x = cmbFontSize.Right + 10;
            AddToolbarButton(btnBold, ref x);
            AddToolbarButton(btnItalic, ref x);
            AddToolbarButton(btnUnderline, ref x);
            x += 10;
            AddToolbarButton(btnBullet, ref x);
            x += 10;
            AddToolbarButton(btnAlignLeft, ref x);
            AddToolbarButton(btnAlignCenter, ref x);
            AddToolbarButton(btnAlignRight, ref x);

            editorToolbar.Controls.Add(cmbFontName);
            editorToolbar.Controls.Add(cmbFontSize);
        }

        private Button MakeTbButton(string text)
        {
            return new Button
            {
                Text = text,
                Width = 34,
                Height = 26,
                Top = 5,
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.White,
                ForeColor = Color.Black,
                Font = new Font("Segoe UI", 10, FontStyle.Bold),
                TabStop = false
            };
        }

        private void AddToolbarButton(Button b, ref int x)
        {
            b.Left = x;
            editorToolbar.Controls.Add(b);
            x += b.Width + 6;
        }

        private void ToggleStyle(FontStyle style)
        {
            var selFont = rtbContent.SelectionFont ?? rtbContent.Font;
            var newStyle = selFont.Style ^ style; // toggle
            rtbContent.SelectionFont = new Font(selFont, newStyle);
            isDirty = true;
            SyncToolbarWithSelection();
        }

        private void ApplyFontFromToolbar()
        {
            string face = cmbFontName.SelectedItem?.ToString() ?? rtbContent.Font.FontFamily.Name;
            float size = float.TryParse(cmbFontSize.SelectedItem?.ToString(), out var s) ? s : rtbContent.Font.Size;

            var selFont = rtbContent.SelectionFont ?? rtbContent.Font;
            var style = selFont.Style;
            rtbContent.SelectionFont = new Font(face, size, style);
            isDirty = true;
            SyncToolbarWithSelection();
        }

        private void SyncToolbarWithSelection()
        {
            var f = rtbContent.SelectionFont ?? rtbContent.Font;
            btnBold.BackColor = f.Bold ? Color.LightSteelBlue : Color.White;
            btnItalic.BackColor = f.Italic ? Color.LightSteelBlue : Color.White;
            btnUnderline.BackColor = f.Underline ? Color.LightSteelBlue : Color.White;

            // keep comboboxes in sync if possible
            if (cmbFontName.Items.Contains(f.FontFamily.Name)) cmbFontName.SelectedItem = f.FontFamily.Name;
            var s = ((int)Math.Round(f.Size)).ToString();
            if (cmbFontSize.Items.Contains(s)) cmbFontSize.SelectedItem = s;
        }

        private Label CreateLabel(string text, int left, int top) => new Label
        {
            Text = text,
            Left = left,
            Top = top,
            Width = 100,
            TextAlign = ContentAlignment.TopRight,
            Font = new Font("Segoe UI", 10, FontStyle.Bold),
            ForeColor = ColorTranslator.FromHtml("#34495E")
        };

        private TextBox CreateTextBox(int left, int top, int width) => new TextBox
        {
            Left = left,
            Top = top,
            Width = width,
            Height = 28,
            BorderStyle = BorderStyle.FixedSingle,
            Font = new Font("Segoe UI", 10),
            BackColor = Color.White,
            ForeColor = ColorTranslator.FromHtml("#333")
        };

        private Button CreateButton(string text, int left, int top, string normalHex, string hoverHex)
        {
            var btn = new Button
            {
                Text = text,
                Left = left,
                Top = top,
                Width = 140,
                Height = 40,
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

        private Button CreateUtilityButton(string text, int left, int top) => new Button
        {
            Text = text,
            Left = left,
            Top = top,
            Width = 80,
            Height = 28,
            FlatStyle = FlatStyle.Flat,
            BackColor = Color.Gainsboro,
            ForeColor = Color.Black,
            Font = new Font("Segoe UI", 9),
            Cursor = Cursors.Hand,
            TabStop = false
        };

        private void UpdateCounter() => lblCounter.Text = $"Characters: {rtbContent.TextLength}";

        private async System.Threading.Tasks.Task LoadCategoriesFromDbAsync()
        {
            try
            {
                using var ctx = new NotesDbContext();
                var cats = await ctx.Notes.AsNoTracking()
                    .Select(n => n.Category)
                    .Where(c => c != null && c != "")
                    .Distinct()
                    .OrderBy(c => c)
                    .ToListAsync();
                if (cats.Count > 0) categorySelector.SetCategories(cats);
            }
            catch { }
        }

        private void PopulateFromNote()
        {
            if (currentNote == null) return;

            Text = $"Edit Note: {currentNote.Title}";
            txtTitle.Text = currentNote.Title;
            categorySelector.SelectedCategory = currentNote.Category;

            // لو عندنا RTF محفوظ استخدمه، غير كده اعرض النص العادي
            if (!string.IsNullOrEmpty(currentNote.ContentRtf))
                rtbContent.Rtf = currentNote.ContentRtf;
            else
                rtbContent.Text = currentNote.Content;

            if (currentNote.ReminderDate.HasValue)
            {
                chkHasReminder.Checked = true;
                dtpReminder.Enabled = true;
                dtpReminder.Value = currentNote.ReminderDate.Value;
            }
            else
            {
                chkHasReminder.Checked = false;
                dtpReminder.Enabled = false;
            }
            isDirty = false;
            UpdateCounter();
        }


        private async void BtnSave_Click(object? sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtTitle.Text))
            {
                MessageBox.Show("Title is required.", "Validation", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            if (!AppSession.IsAuthenticated)
            {
                MessageBox.Show("Please login first.", "Auth", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            try
            {
                isSaving = true; // <=== التعديل الثاني: الإعلان عن بدء عملية الحفظ

                if (currentNote == null)
                {
                    currentNote = new Note
                    {
                        Title = txtTitle.Text.Trim(),
                        Category = categorySelector.SelectedCategory ?? string.Empty,
                        Content = rtbContent.Text,      // نص للبحث والفلاتر
                        ContentRtf = rtbContent.Rtf,    // <<— RTF للتنسيق
                        CreationDate = DateTime.Now,
                        ReminderDate = chkHasReminder.Checked ? dtpReminder.Value : null,
                        IsReminderShown = false,
                        UserID = AppSession.CurrentUser!.UserID
                    };
                    _context.Notes.Add(currentNote);
                }
                else
                {
                    currentNote.Title = txtTitle.Text.Trim();
                    currentNote.Category = categorySelector.SelectedCategory ?? string.Empty;
                    currentNote.Content = rtbContent.Text;
                    currentNote.ContentRtf = rtbContent.Rtf;      // <<— حدّث RTF
                    currentNote.ReminderDate = chkHasReminder.Checked ? dtpReminder.Value : null;
                    currentNote.IsReminderShown = false;
                    _context.Notes.Update(currentNote);
                }

                await _context.SaveChangesAsync();
                isDirty = false; // تم الحفظ بنجاح، لا توجد تغييرات لتجاهلها

                await LoadCategoriesFromDbAsync();
                NoteSaved?.Invoke(currentNote);

                // رسالة النجاح ستظهر قبل الإغلاق
                MessageBox.Show("Saved ✅", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);

                if (this.Modal) DialogResult = DialogResult.OK;
                Close();
            }
            catch (DbUpdateException ex)
            {
                isSaving = false; // فشل الحفظ، لإعادة تمكين التحقق
                MessageBox.Show($"DB error: {ex.GetBaseException().Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            catch (Exception ex)
            {
                isSaving = false; // فشل الحفظ، لإعادة تمكين التحقق
                MessageBox.Show($"Unexpected: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                // تأكد من تعيين isSaving إلى false إذا لم يتم الإغلاق لأي سبب
                // لكن الإغلاق يحدث في Try، لذا سنترك إعادة التعيين في OnFormClosing
                // أو نضعه هنا للتأكد في حالة فشل جزء من العملية
                if (isDirty) isSaving = false;
            }
        }


        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            // <=== التعديل الثالث: أضفنا شرط && !isSaving
            if (e.CloseReason == CloseReason.UserClosing && isDirty && !isSaving)
            {
                var res = MessageBox.Show("Discard unsaved changes?", "Confirm", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
                if (res == DialogResult.No) { e.Cancel = true; return; }
            }

            // في حال تم إلغاء الإغلاق أو حدث خطأ وتم الإغلاق بالـ ESC
            isSaving = false;

            base.OnFormClosing(e);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing) _context?.Dispose();
            base.Dispose(disposing);
        }
    }
}