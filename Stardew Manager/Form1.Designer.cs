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
                BackColor = Color.LightGray
            };

            PictureBox pictureBox = new PictureBox
            {
                Size = new Size(100, 100),
                Location = new Point(10, 10),
                SizeMode = PictureBoxSizeMode.StretchImage,
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

            Panel panelStrip = new Panel
            {
                Size = new Size(1980, 120),
                Location = new Point(0, 0),
                BackColor = Color.LightBlue
            };

            Label StardewManager = new Label
            {
                Text = "Stardew Manager",
                Location = new Point(120, 35),
                AutoSize = true,
                Font = new Font("Arial", 24, FontStyle.Bold)
            };

            Label Version = new Label
            {
                Text = "0.5.0",
                Location = new Point(1840, 10),
                AutoSize = true,
                Font = new Font("Arial", 12, FontStyle.Bold)
            };

            SeasonLabel = new Label
            {
                Text = "Сезон",
                Location = new Point(20, 135),
                AutoSize = true,
                Font = new Font("Arial", 12, FontStyle.Bold)
            };

            SeasonComboBox = new ComboBox
            {
                Location = new Point(180, 135),
                Size = new Size(100, 30),
                DropDownStyle = ComboBoxStyle.DropDownList,
                Font = new Font("Arial", 10, FontStyle.Regular)
            };
            SeasonComboBox.SelectedIndexChanged += OnSeasonChanged;

            Label Quantity = new Label
            {
                Text = "Кол-во грядок",
                Location = new Point(20, 170),
                AutoSize = true,
                Font = new Font("Arial", 12, FontStyle.Bold)
            };

            QuantityTextBox = new TextBox
            {
                Size = new Size(100, 40),
                Location = new Point(180, 170),
                Text = "20",
                TextAlign = HorizontalAlignment.Center
            };

            Label MoneyQuantity = new Label
            {
                Text = "Кол-во денег",
                Location = new Point(20, 205),
                AutoSize = true,
                Font = new Font("Arial", 12, FontStyle.Bold)
            };

            MoneyQuantityTextBox = new TextBox
            {
                Size = new Size(100, 40),
                Location = new Point(180, 205),
                Text = "1000",
                TextAlign = HorizontalAlignment.Center
            };

            CalculateButton = new Button
            {
                Text = "Рассчитать",
                Location = new Point(20, 245),
                Size = new Size(120, 30),
                Font = new Font("Arial", 10, FontStyle.Bold),
                BackColor = Color.White
            };
            CalculateButton.Click += OnCalculateButtonClick;

            FertilizerButton = new Button
            {
                Text = "Удобрения и ресурсы",
                Location = new Point(150, 245),
                Size = new Size(250, 30),
                Font = new Font("Arial", 10, FontStyle.Bold),
                BackColor = Color.LightYellow
            };
            FertilizerButton.Click += OnFertilizerButtonClick;

            ResultSummaryLabel = new Label
            {
                Text = "",
                Location = new Point(700, 250),
                AutoSize = true,
                Font = new Font("Arial", 11, FontStyle.Bold),
                ForeColor = Color.DarkGreen
            };

            Data = new DataGridView()
            {
                Size = new Size(500, 450),
                Location = new Point(20, 290),
                BorderStyle = BorderStyle.FixedSingle,
                BackgroundColor = Color.White,
                AllowUserToAddRows = false,
                ReadOnly = true,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill
            };

            panelStrip.Controls.Add(pictureBox);
            panelStrip.Controls.Add(StardewManager);
            panelStrip.Controls.Add(Version);

            cardPanel.Controls.Add(panelStrip);
            cardPanel.Controls.Add(SeasonLabel);
            cardPanel.Controls.Add(SeasonComboBox);
            cardPanel.Controls.Add(Quantity);
            cardPanel.Controls.Add(QuantityTextBox);
            cardPanel.Controls.Add(MoneyQuantity);
            cardPanel.Controls.Add(MoneyQuantityTextBox);
            cardPanel.Controls.Add(CalculateButton);
            cardPanel.Controls.Add(FertilizerButton);
            cardPanel.Controls.Add(ResultSummaryLabel);
            cardPanel.Controls.Add(Data);

            this.Controls.Add(cardPanel);
            this.WindowState = FormWindowState.Maximized;
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