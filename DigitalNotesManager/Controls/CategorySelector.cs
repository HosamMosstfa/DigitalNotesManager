using System;
using System.Collections.Generic;
using System.Windows.Forms;

namespace DigitalNotesManager.Controls
{
    public class CategorySelector : UserControl
    {
        private ComboBox combo;
        private Button btnManage;

        public event Action<string>? CategoryChanged;

        public CategorySelector()
        {
            InitializeControl();
            LoadDefaultCategories();
        }

        private void InitializeControl()
        {
            Width = 260;
            Height = 28;

            combo = new ComboBox
            {
                Left = 0,
                Top = 0,
                Width = 200,
                DropDownStyle = ComboBoxStyle.DropDown
            };
            combo.SelectedIndexChanged += (s, e) => CategoryChanged?.Invoke(SelectedCategory ?? string.Empty);

            btnManage = new Button
            {
                Text = "...",
                Left = combo.Right + 6,
                Top = 0,
                Width = 40,
                Height = combo.Height
            };
            btnManage.Click += BtnManage_Click;

            Controls.Add(combo);
            Controls.Add(btnManage);
        }
        private readonly List<string> _defaultCategories = new List<string> { "General", "Personal", "Work", "Study", "Ideas" };
        private void LoadDefaultCategories()
        {
            combo.Items.Clear();
            foreach (var c in _defaultCategories) combo.Items.Add(c);
            if (combo.Items.Count > 0) combo.SelectedIndex = 0;
        }

        private void BtnManage_Click(object? sender, EventArgs e)
        {
            using (var input = new Form())
            {
                input.Text = "Add Category";
                input.Width = 300;
                input.Height = 140;
                input.StartPosition = FormStartPosition.CenterParent;

                var tb = new TextBox { Left = 12, Top = 12, Width = 250, Height = 50 };
                var ok = new Button { Text = "Add", Left = 12, Top = 50, Width = 80, Height = 30, DialogResult = DialogResult.OK };
                input.Controls.Add(tb);
                input.Controls.Add(ok);
                input.AcceptButton = ok;

                if (input.ShowDialog() == DialogResult.OK)
                {
                    var val = tb.Text.Trim();
                    if (!string.IsNullOrEmpty(val) && !combo.Items.Contains(val))
                    {
                        combo.Items.Add(val);
                        combo.SelectedItem = val;
                        CategoryChanged?.Invoke(val);
                    }
                }
            }
        }

        public string? SelectedCategory
        {
            get => combo.SelectedItem?.ToString();
            set
            {
                if (value == null) { combo.SelectedIndex = -1; return; }
                if (!combo.Items.Contains(value)) combo.Items.Add(value);
                combo.SelectedItem = value;
            }
        }

        public void SetCategories(IEnumerable<string> categories)
        {

            var mergedCategories = _defaultCategories
                .Union(categories)
                .Distinct() // إزالة أي تكرار
                .OrderBy(c => c)
                .ToList();

            var currentSelection = combo.Text; 

            combo.Items.Clear();
            foreach (var c in mergedCategories) combo.Items.Add(c);

            // محاولة استعادة القيمة المحددة مسبقاً
            if (!string.IsNullOrEmpty(currentSelection) && combo.Items.Contains(currentSelection))
            {
                combo.SelectedItem = currentSelection;
            }
            else if (combo.Items.Count > 0)
            {
                combo.SelectedIndex = 0;
            }
        }
    }
}
