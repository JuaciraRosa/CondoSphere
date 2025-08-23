using CondoSphereWinForms.Models;
using CondoSphereWinForms.Services;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace CondoSphereWinForms.Forms
{
    public partial class MainForm : Form
    {
        private DataGridView dgv;
        private BindingList<Condominium> _data = new();
        private Button btnLoad, btnAdd, btnDelete, btnLogout, btnRefresh;

        public MainForm()
        {
            this.Text = "CondoSphere - Condominiums";
            this.Width = 800;
            this.Height = 520;
            this.StartPosition = FormStartPosition.CenterScreen;

            dgv = new DataGridView
            {
                Dock = DockStyle.Top,
                Height = 380,
                ReadOnly = true,
                AutoGenerateColumns = true,
                DataSource = _data,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect
            };

            btnLoad = new Button { Text = "Load", Left = 20, Top = 400, Width = 100 };
            btnRefresh = new Button { Text = "Refresh", Left = 130, Top = 400, Width = 100 };
            btnAdd = new Button { Text = "Add", Left = 240, Top = 400, Width = 100 };
            btnDelete = new Button { Text = "Delete", Left = 350, Top = 400, Width = 100 };
            btnLogout = new Button { Text = "Logout", Left = 670, Top = 400, Width = 100 };

            btnLoad.Click += async (_, __) => await LoadDataAsync();
            btnRefresh.Click += async (_, __) => await LoadDataAsync();
            btnAdd.Click += async (_, __) => await AddAsync();
            btnDelete.Click += async (_, __) => await DeleteAsync();
            btnLogout.Click += (_, __) => DoLogout();

            this.Controls.AddRange(new Control[] { dgv, btnLoad, btnRefresh, btnAdd, btnDelete, btnLogout });

            this.Shown += async (_, __) => await LoadDataAsync();
        }

        private void DoLogout()
        {
            ApiClient.SetToken(null);
            var login = new LoginForm();
            login.FormClosed += (_, __) => this.Close();
            login.Show();
            this.Hide();
        }

        private async Task LoadDataAsync()
        {
            try
            {
                var list = await ApiClient.GetAsync<List<Condominium>>("condominiums");
                _data.Clear();
                foreach (var item in list) _data.Add(item);
            }
            catch (UnauthorizedAccessException)
            {
                MessageBox.Show("Session expired. Please log in again.", "Unauthorized", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                DoLogout();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Error loading data", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private async Task AddAsync()
        {
            using var dlg = new AddCondominiumForm();
            if (dlg.ShowDialog(this) == DialogResult.OK)
            {
                try
                {
                    // POST /api/condominiums
                    await ApiClient.PostAsync<Condominium>("condominiums", dlg.Result);
                    await LoadDataAsync();
                }
                catch (UnauthorizedAccessException)
                {
                    MessageBox.Show("Session expired. Please log in again.", "Unauthorized", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    DoLogout();
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message, "Error creating", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        private async Task DeleteAsync()
        {
            if (dgv.CurrentRow?.DataBoundItem is not Condominium selected)
            {
                MessageBox.Show("Select a row first.", "Delete", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            if (MessageBox.Show($"Delete '{selected.Name}'?", "Confirm", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
            {
                try
                {
                    // DELETE /api/condominiums/{id}
                    await ApiClient.DeleteAsync($"condominiums/{selected.Id}");
                    await LoadDataAsync();
                }
                catch (UnauthorizedAccessException)
                {
                    MessageBox.Show("Session expired. Please log in again.", "Unauthorized", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    DoLogout();
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message, "Error deleting", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }
    }
}
