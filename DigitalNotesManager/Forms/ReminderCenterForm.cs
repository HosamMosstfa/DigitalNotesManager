using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using DigitalNotesManager.Data;
using Microsoft.EntityFrameworkCore;
using DigitalNotesManager.Security;

namespace DigitalNotesManager.Forms
{
    public class ReminderCenterForm : Form
    {
        private DataGridView dgvReminders;
        private ComboBox cmbFilter;
        private Button btnMarkDone, btnEdit, btnClose, btnReload;
        private readonly NotesDbContext _context = new NotesDbContext();

        public ReminderCenterForm()
        {
            InitializeForm();
            LoadReminders();
        }

        private void InitializeForm()
        {
            Text = "🔔 Reminder Center";
            Width = 900; Height = 560;
            StartPosition = FormStartPosition.CenterParent;
            BackColor = ColorTranslator.FromHtml("#F5F7FB");
            Font = new Font("Segoe UI", 10);
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;

            int margin = 20, topY = 20;

            var lblFilter = new Label
            {
                Text = "Filter by Status:",
                Left = margin,
                Top = topY + 5,
                Width = 120,
                Font = new Font("Segoe UI", 10, FontStyle.Bold),
                ForeColor = ColorTranslator.FromHtml("#34495E")
            };
            cmbFilter = new ComboBox
            {
                Left = lblFilter.Left + lblFilter.Width + 5,
                Top = topY,
                Width = 220,
                DropDownStyle = ComboBoxStyle.DropDownList
            };
            cmbFilter.Items.AddRange(new object[] { "All Reminders", "Upcoming ⏳", "Done ✅" });
            cmbFilter.SelectedIndex = 0;
            cmbFilter.SelectedIndexChanged += (s, e) => LoadReminders();

            dgvReminders = new DataGridView
            {
                Left = margin,
                Top = topY + 50,
                Width = ClientSize.Width - 2 * margin,
                Height = 360,
                ReadOnly = true,
                AutoGenerateColumns = true,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                MultiSelect = false,
                AllowUserToAddRows = false,
                BackgroundColor = Color.White,
                BorderStyle = BorderStyle.FixedSingle,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Bottom
            };
            dgvReminders.DefaultCellStyle.BackColor = Color.White;
            dgvReminders.DefaultCellStyle.ForeColor = Color.Black;
            dgvReminders.DefaultCellStyle.SelectionBackColor = ColorTranslator.FromHtml("#D2E1FA");
            dgvReminders.DefaultCellStyle.SelectionForeColor = Color.Black;
            dgvReminders.ColumnHeadersDefaultCellStyle.BackColor = ColorTranslator.FromHtml("#E3ECFC");
            dgvReminders.ColumnHeadersDefaultCellStyle.ForeColor = ColorTranslator.FromHtml("#1E3C72");
            dgvReminders.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI", 10, FontStyle.Bold);
            dgvReminders.EnableHeadersVisualStyles = false;
            dgvReminders.RowTemplate.Height = 30;
            dgvReminders.CellDoubleClick += (s, e) => BtnEdit_Click(s, EventArgs.Empty);

            int buttonY = dgvReminders.Top + dgvReminders.Height + 20;
            btnMarkDone = CreateBtn("✅ Mark Done", "#4CAF50", "#388E3C"); btnMarkDone.Left = margin; btnMarkDone.Top = buttonY; btnMarkDone.Click += BtnMarkDone_Click;
            btnEdit = CreateBtn("✏️ Edit Note", "#FFC107", "#FFB300"); btnEdit.Left = btnMarkDone.Left + 162; btnEdit.Top = buttonY; btnEdit.Click += BtnEdit_Click;
            btnReload = CreateBtn("🔄 Reload", "#90CAF9", "#64B5F6"); btnReload.Left = btnEdit.Left + 162; btnReload.Top = buttonY; btnReload.Click += (s, e) => LoadReminders();
            btnClose = CreateBtn("❌ Close", "#78909C", "#546E7A"); btnClose.Width = 140; btnClose.Left = ClientSize.Width - btnClose.Width - margin; btnClose.Top = buttonY; btnClose.Anchor = AnchorStyles.Bottom | AnchorStyles.Right; btnClose.Click += (s, e) => Close();

            Controls.AddRange(new Control[] { lblFilter, cmbFilter, dgvReminders, btnMarkDone, btnEdit, btnReload, btnClose });

            Resize += (s, e) =>
            {
                dgvReminders.Width = ClientSize.Width - 2 * margin;
                dgvReminders.Height = ClientSize.Height - (dgvReminders.Top + 100);
                int newY = dgvReminders.Top + dgvReminders.Height + 20;
                btnMarkDone.Top = btnEdit.Top = btnReload.Top = btnClose.Top = newY;
                btnClose.Left = ClientSize.Width - btnClose.Width - margin;
            };
        }

        private Button CreateBtn(string text, string normal, string hover)
        {
            var b = new Button
            {
                Text = text,
                Width = 150,
                Height = 40,
                FlatStyle = FlatStyle.Flat,
                BackColor = ColorTranslator.FromHtml(normal),
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 10, FontStyle.Bold),
                Cursor = Cursors.Hand,
                Anchor = AnchorStyles.Bottom | AnchorStyles.Left
            };
            b.FlatAppearance.BorderSize = 0;
            b.MouseEnter += (s, e) => b.BackColor = ColorTranslator.FromHtml(hover);
            b.MouseLeave += (s, e) => b.BackColor = ColorTranslator.FromHtml(normal);
            return b;
        }

        private async void LoadReminders()
        {
            if (!AppSession.IsAuthenticated) { dgvReminders.DataSource = null; return; }
            string filter = cmbFilter?.SelectedItem?.ToString() ?? "All Reminders";
            var now = DateTime.Now;
            var userId = AppSession.CurrentUser!.UserID;

            var q = _context.Notes.AsNoTracking().Where(n => n.UserID == userId && n.ReminderDate.HasValue);
            if (filter.Contains("Upcoming")) q = q.Where(n => n.ReminderDate > now && !n.IsReminderShown);
            else if (filter.Contains("Done")) q = q.Where(n => n.IsReminderShown);

            var data = await q.OrderBy(n => n.ReminderDate).Select(n => new
            {
                n.NoteID,
                n.Title,
                n.Category,
                ReminderTime = n.ReminderDate!.Value.ToString("yyyy-MM-dd HH:mm"),
                Status = n.IsReminderShown ? "Completed ✅" : (n.ReminderDate <= now ? "Overdue 🔴" : "Upcoming ⏳")
            }).ToListAsync();

            dgvReminders.DataSource = data;
            if (dgvReminders.Columns.Contains("NoteID")) dgvReminders.Columns["NoteID"].Visible = false;
            if (dgvReminders.Columns.Contains("Title")) dgvReminders.Columns["Title"].HeaderText = "Note Title";
            if (dgvReminders.Columns.Contains("ReminderTime")) dgvReminders.Columns["ReminderTime"].HeaderText = "Time";

            // soft row-highlights
            foreach (DataGridViewRow row in dgvReminders.Rows)
            {
                var status = row.Cells["Status"]?.Value?.ToString() ?? "";
                if (status.Contains("Overdue")) row.DefaultCellStyle.BackColor = Color.MistyRose;
                else if (status.Contains("Upcoming")) row.DefaultCellStyle.BackColor = Color.LemonChiffon;
                else if (status.Contains("Completed")) row.DefaultCellStyle.BackColor = Color.Honeydew;
            }
        }

        private int? GetSelectedId()
        {
            if (dgvReminders.CurrentRow == null || !dgvReminders.Columns.Contains("NoteID")) return null;
            var s = dgvReminders.CurrentRow.Cells["NoteID"].Value?.ToString();
            return int.TryParse(s, out var id) ? id : (int?)null;
        }

        private async void BtnMarkDone_Click(object? sender, EventArgs e)
        {
            var id = GetSelectedId();
            if (id == null) { MessageBox.Show("Select a reminder first."); return; }

            var note = await _context.Notes.FirstOrDefaultAsync(n => n.NoteID == id.Value);
            if (note == null) return;

            if (note.IsReminderShown) { MessageBox.Show("Already marked done."); return; }
            note.IsReminderShown = true;
            await _context.SaveChangesAsync();
            LoadReminders();
        }

        private async void BtnEdit_Click(object? sender, EventArgs e)
        {
            var id = GetSelectedId();
            if (id == null) { MessageBox.Show("Select a reminder to edit."); return; }

            var note = await _context.Notes.FirstOrDefaultAsync(n => n.NoteID == id.Value);
            if (note == null) return;

            using var f = new NoteForm(note);
            f.NoteSaved += async (upd) =>
            {
                _context.Entry(upd).State = EntityState.Modified;
                await _context.SaveChangesAsync();
                LoadReminders();
            };
            f.ShowDialog(this);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing) _context?.Dispose();
            base.Dispose(disposing);
        }
    }
}
