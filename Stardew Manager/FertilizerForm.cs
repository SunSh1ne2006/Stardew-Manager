using System;
using System.Collections.Generic;
using System.Data;
using System.Windows.Forms;

namespace Stardew_Manager
{
    public partial class FertilizerForm : Form
    {
        private readonly DatabaseService _dbService;
        private List<IngredientItem> _inventory;

        public FertilizerForm(DatabaseService dbService)
        {
            _dbService = dbService;
            InitializeCustomCard();
            this.Load += OnFormLoad;
        }

        private async void OnFormLoad(object sender, EventArgs e)
        {
            try
            {
                _inventory = await _dbService.GetUserInventoryAsync();

                DataTable table = new DataTable();
                table.Columns.Add("ID", typeof(int));
                table.Columns.Add("Название ресурса", typeof(string));
                table.Columns.Add("Количество (шт)", typeof(int));

                foreach (var item in _inventory)
                {
                    table.Rows.Add(item.Id, item.Name, item.AvailableQuantity);
                }

                _inventoryGrid.DataSource = table;
                _inventoryGrid.Columns["ID"].Visible = false;
                _inventoryGrid.Columns["Название ресурса"].ReadOnly = true;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки инвентаря: {ex.Message}", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private async void OnSaveButtonClick(object sender, EventArgs e)
        {
            try
            {
                var updatedItems = new List<IngredientItem>();
                DataTable table = (DataTable)_inventoryGrid.DataSource;

                foreach (DataRow row in table.Rows)
                {
                    updatedItems.Add(new IngredientItem
                    {
                        Id = Convert.ToInt32(row["ID"]),
                        Name = row["Название ресурса"].ToString(),
                        AvailableQuantity = Convert.ToInt32(row["Количество (шт)"])
                    });
                }

                await _dbService.SaveUserInventoryAsync(updatedItems);
                MessageBox.Show("Данные инвентаря успешно сохранены!", "Успешно", MessageBoxButtons.OK, MessageBoxIcon.Information);
                this.DialogResult = DialogResult.OK;
                this.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка сохранения: {ex.Message}", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}