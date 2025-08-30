using CondoSphereWinForms.Models;
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
    public partial class AddCondominiumForm : Form
    {
        private TextBox txtName;
        private TextBox txtAddress;
        private NumericUpDown numCompanyId;
        private Button btnOk;
        private Button btnCancel;

        public Condominium Result { get; private set; }

        public AddCondominiumForm()
        {
            this.Text = "Add Condominium";
            this.Width = 420;
            this.Height = 260;
            this.StartPosition = FormStartPosition.CenterParent;

            var lblN = new Label { Text = "Name", Left = 20, Top = 20, Width = 360 };
            txtName = new TextBox { Left = 20, Top = 40, Width = 360 };

            var lblA = new Label { Text = "Address", Left = 20, Top = 75, Width = 360 };
            txtAddress = new TextBox { Left = 20, Top = 95, Width = 360 };

            var lblC = new Label { Text = "CompanyId", Left = 20, Top = 130, Width = 360 };
            numCompanyId = new NumericUpDown { Left = 20, Top = 150, Width = 100, Minimum = 1, Maximum = 100000 };

            btnOk = new Button { Text = "Create", Left = 200, Top = 180, Width = 80 };
            btnCancel = new Button { Text = "Cancel", Left = 300, Top = 180, Width = 80 };

            btnOk.Click += (_, __) =>
            {
                Result = new Condominium
                {
                    Name = txtName.Text?.Trim(),
                    Address = txtAddress.Text?.Trim(),
                    CompanyId = (int)numCompanyId.Value
                };
                this.DialogResult = DialogResult.OK;
                this.Close();
            };
            btnCancel.Click += (_, __) =>
            {
                this.DialogResult = DialogResult.Cancel;
                this.Close();
            };

            this.Controls.AddRange(new Control[] { lblN, txtName, lblA, txtAddress, lblC, numCompanyId, btnOk, btnCancel });
        }
    }
}
