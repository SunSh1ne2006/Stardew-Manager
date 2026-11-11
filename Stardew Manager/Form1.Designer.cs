using System;
using System.Drawing;
using System.IO;
using System.Windows.Forms;

namespace Stardew_Manager
{
    partial class Form1
    {
        private System.ComponentModel.IContainer components = null;

        private void InitializeCustomCard()
        {
            Panel cardPanel = new Panel
            {
                Size = new Size(1980, 1280),
                BorderStyle = BorderStyle.None,
                BackColor = Color.FromArgb(243, 245, 248)
            };

            /* ---------------- Шапка ---------------- */
            Panel panelStrip = new Panel
            {
                Size = new Size(1980, 120),
                Location = new Point(0, 0),
                BackColor = Color.FromArgb(226, 238, 249)
            };

            PictureBox pictureBox = new PictureBox
            {
                Size = new Size(100, 100),
                Location = new Point(12, 10),
                SizeMode = PictureBoxSizeMode.Zoom,
                BackColor = Color.Transparent
            };

            string imagePath = Path.Combine(Application.StartupPath, "Images", "logo.jpg");
            if (File.Exists(imagePath))
            {
                try
                {
                    pictureBox.Image = Image.FromFile(imagePath);
                }
                catch
                {

                }
            }

            Label StardewManager = new Label
            {
                Text = "Stardew Manager",
                Location = new Point(126, 20),
                AutoSize = true,
                Font = new Font("Segoe UI", 22, FontStyle.Bold),
                ForeColor = Color.FromArgb(30, 45, 60)
            };

            Label Subtitle = new Label
            {
                Text = "Планировщик посадок: сам подбирает культуры и удобрения под ваш бюджет",
                Location = new Point(130, 72),
                AutoSize = true,
                Font = new Font("Segoe UI", 10, FontStyle.Regular),
                ForeColor = Color.FromArgb(70, 90, 110)
            };

            Label Version = new Label
            {
                Text = "0.7.0",
                Location = new Point(1840, 14),
                AutoSize = true,
                Font = new Font("Segoe UI", 11, FontStyle.Bold),
                ForeColor = Color.FromArgb(70, 90, 110)
            };

            panelStrip.Controls.Add(pictureBox);
            panelStrip.Controls.Add(StardewManager);
            panelStrip.Controls.Add(Subtitle);
            panelStrip.Controls.Add(Version);

            /* ---------------- Параметры ---------------- */
            Panel paramsPanel = new Panel
            {
                Location = new Point(20, 140),
                Size = new Size(470, 205),
                BackColor = Color.White,
                BorderStyle = BorderStyle.FixedSingle
            };

            Label paramsTitle = new Label
            {
                Text = "Параметры сезона",
                Location = new Point(14, 10),
                AutoSize = true,
                Font = new Font("Segoe UI", 11, FontStyle.Bold),
                ForeColor = Color.FromArgb(30, 45, 60)
            };

            SeasonLabel = new Label
            {
                Text = "Сезон",
                Location = new Point(14, 52),
                AutoSize = true,
                Font = new Font("Segoe UI", 10, FontStyle.Regular)
            };

            SeasonComboBox = new ComboBox
            {
                Location = new Point(150, 48),
                Size = new Size(300, 30),
                DropDownStyle = ComboBoxStyle.DropDownList,
                Font = new Font("Segoe UI", 10, FontStyle.Regular)
            };
            SeasonComboBox.SelectedIndexChanged += OnSeasonChanged;

            Label Quantity = new Label
            {
                Text = "Количество грядок",
                Location = new Point(14, 92),
                AutoSize = true,
                Font = new Font("Segoe UI", 10, FontStyle.Regular)
            };

            QuantityTextBox = new TextBox
            {
                Location = new Point(150, 88),
                Size = new Size(120, 27),
                Text = "20",
                TextAlign = HorizontalAlignment.Center,
                Font = new Font("Segoe UI", 10, FontStyle.Regular)
            };

            Label MoneyQuantity = new Label
            {
                Text = "Бюджет, g",
                Location = new Point(14, 132),
                AutoSize = true,
                Font = new Font("Segoe UI", 10, FontStyle.Regular)
            };

            MoneyQuantityTextBox = new TextBox
            {
                Location = new Point(150, 128),
                Size = new Size(120, 27),
                Text = "1000",
                TextAlign = HorizontalAlignment.Center,
                Font = new Font("Segoe UI", 10, FontStyle.Regular)
            };

            Label Hint = new Label
            {
                Text = "Удобрения — автоматически",
                Location = new Point(285, 131),
                AutoSize = true,
                Font = new Font("Segoe UI", 8, FontStyle.Italic),
                ForeColor = Color.FromArgb(120, 135, 150)
            };

            CalculateButton = new Button
            {
                Text = "Рассчитать план",
                Location = new Point(14, 162),
                Size = new Size(436, 32),
                Font = new Font("Segoe UI", 10, FontStyle.Bold),
                BackColor = Color.FromArgb(226, 240, 226),
                FlatStyle = FlatStyle.Flat
            };
            CalculateButton.FlatAppearance.BorderColor = Color.FromArgb(180, 205, 180);
            CalculateButton.Click += OnCalculateButtonClick;

            paramsPanel.Controls.Add(paramsTitle);
            paramsPanel.Controls.Add(SeasonLabel);
            paramsPanel.Controls.Add(SeasonComboBox);
            paramsPanel.Controls.Add(Quantity);
            paramsPanel.Controls.Add(QuantityTextBox);
            paramsPanel.Controls.Add(MoneyQuantity);
            paramsPanel.Controls.Add(MoneyQuantityTextBox);
            paramsPanel.Controls.Add(Hint);
            paramsPanel.Controls.Add(CalculateButton);

            /* ---------------- Итоги ---------------- */
            Panel summaryPanel = new Panel
            {
                Location = new Point(500, 140),
                Size = new Size(1460, 205),
                BackColor = Color.White,
                BorderStyle = BorderStyle.FixedSingle
            };

            Label summaryTitle = new Label
            {
                Text = "Итоги плана",
                Location = new Point(14, 10),
                AutoSize = true,
                Font = new Font("Segoe UI", 11, FontStyle.Bold),
                ForeColor = Color.FromArgb(30, 45, 60)
            };

            ResultSummaryLabel = new Label
            {
                Text = "Выберите сезон, укажите грядки и бюджет, затем нажмите «Рассчитать план».\r\n"
                     + "Программа сама решит, какие культуры сажать и на какие из них стоит тратиться на удобрения.",
                Location = new Point(14, 44),
                MaximumSize = new Size(1430, 0),
                AutoSize = true,
                Font = new Font("Segoe UI", 10, FontStyle.Regular),
                ForeColor = Color.FromArgb(40, 60, 50)
            };

            Label Legend = new Label
            {
                Text = "Как считаем: ускорение удобрения действует только до первого урожая, качество — на все сборы; "
                     + "цена удобрения берётся по самому дешёвому способу (покупка или крафт). Все суммы — в золоте (g).",
                Location = new Point(14, 168),
                MaximumSize = new Size(1430, 0),
                AutoSize = true,
                Font = new Font("Segoe UI", 8, FontStyle.Italic),
                ForeColor = Color.FromArgb(130, 145, 160)
            };

            summaryPanel.Controls.Add(summaryTitle);
            summaryPanel.Controls.Add(ResultSummaryLabel);
            summaryPanel.Controls.Add(Legend);

            /* ---------------- Вкладки с таблицами ---------------- */
            ResultTabs = new TabControl
            {
                Location = new Point(20, 350),
                Size = new Size(1900, 380),
                Font = new Font("Segoe UI", 10, FontStyle.Bold)
            };

            CatalogTab = new TabPage("Культуры сезона")
            {
                BackColor = Color.White,
                Padding = new Padding(0)
            };

            PlanTab = new TabPage("План посадок")
            {
                BackColor = Color.White,
                Padding = new Padding(0)
            };

            CatalogGrid = CreateGrid();
            Data = CreateGrid();

            CatalogTab.Controls.Add(CatalogGrid);
            PlanTab.Controls.Add(Data);

            ResultTabs.TabPages.Add(CatalogTab);
            ResultTabs.TabPages.Add(PlanTab);

            /* ---------------- Автоанализ ---------------- */
            Label analysisTitle = new Label
            {
                Text = "Автоматический анализ удобрений",
                Location = new Point(20, 742),
                AutoSize = true,
                Font = new Font("Segoe UI", 11, FontStyle.Bold),
                ForeColor = Color.FromArgb(30, 45, 60)
            };

            RecommendationLabel = new Label
            {
                Text = "",
                Location = new Point(20, 774),
                MaximumSize = new Size(1900, 0),
                AutoSize = true,
                Font = new Font("Segoe UI", 9, FontStyle.Regular),
                ForeColor = Color.FromArgb(60, 70, 110)
            };

            cardPanel.Controls.Add(panelStrip);
            cardPanel.Controls.Add(paramsPanel);
            cardPanel.Controls.Add(summaryPanel);
            cardPanel.Controls.Add(ResultTabs);
            cardPanel.Controls.Add(analysisTitle);
            cardPanel.Controls.Add(RecommendationLabel);

            this.Controls.Add(cardPanel);
            this.AutoScroll = true;
            this.WindowState = FormWindowState.Maximized;
        }

        /// <summary>Единый стиль для обеих таблиц.</summary>
        private static DataGridView CreateGrid()
        {
            var grid = new DataGridView
            {
                Dock = DockStyle.Fill,
                BorderStyle = BorderStyle.None,
                BackgroundColor = Color.White,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                AllowUserToResizeRows = false,
                ReadOnly = true,
                RowHeadersVisible = false,
                MultiSelect = false,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.AllCells,
                ColumnHeadersHeight = 38,
                ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing,
                EnableHeadersVisualStyles = false,
                GridColor = Color.FromArgb(225, 230, 235),
                Font = new Font("Segoe UI", 10, FontStyle.Regular),
                RowTemplate = { Height = 34 }
            };

            grid.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(238, 243, 248);
            grid.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI", 10, FontStyle.Bold);
            grid.ColumnHeadersDefaultCellStyle.Padding = new Padding(6, 0, 6, 0);
            grid.AlternatingRowsDefaultCellStyle.BackColor = Color.FromArgb(249, 251, 253);
            grid.DefaultCellStyle.SelectionBackColor = Color.FromArgb(206, 228, 248);
            grid.DefaultCellStyle.SelectionForeColor = Color.Black;
            grid.DefaultCellStyle.Padding = new Padding(6, 0, 6, 0);

            return grid;
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}
