using System;
using System.Drawing;
using System.Windows.Forms;

namespace Stardew_Manager
{
    partial class FertilizerForm
    {
        private System.ComponentModel.IContainer components = null;

        private Label _titleLabel;
        private DataGridView _inventoryGrid;
        private Button _saveButton;

        private void InitializeCustomCard()
        {
            Panel cardPanel = new Panel
            {
                Size = new Size(520, 480),
                BorderStyle = BorderStyle.None,
                BackColor = Color.LightGray,
                Dock = DockStyle.Fill
            };

            Panel panelStrip = new Panel
            {
                Size = new Size(520, 60),
                Location = new Point(0, 0),
                BackColor = Color.LightBlue
            };

            _titleLabel = new Label
            {
                Text = "Инвентарь ресурсов",
                Location = new Point(20, 18),
                AutoSize = true,
                Font = new Font("Arial", 16, FontStyle.Bold)
            };

            _inventoryGrid = new DataGridView
            {
                Size = new Size(480, 310),
                Location = new Point(20, 80),
                BorderStyle = BorderStyle.FixedSingle,
                BackgroundColor = Color.White,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill
            };

            _saveButton = new Button
            {
                Text = "Сохранить",
                Location = new Point(360, 405),
                Size = new Size(140, 35),
                Font = new Font("Arial", 10, FontStyle.Bold),
                BackColor = Color.LightGreen
            };
            _saveButton.Click += OnSaveButtonClick;

            panelStrip.Controls.Add(_titleLabel);

            cardPanel.Controls.Add(panelStrip);
            cardPanel.Controls.Add(_inventoryGrid);
            cardPanel.Controls.Add(_saveButton);

            this.Controls.Add(cardPanel);

            this.ClientSize = new Size(520, 460);
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.StartPosition = FormStartPosition.CenterParent;
            this.Text = "Инвентарь и удобрения";
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