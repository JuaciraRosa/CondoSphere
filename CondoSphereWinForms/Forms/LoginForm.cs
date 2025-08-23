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
    public partial class LoginForm : Form
    {
        public LoginForm()
        {
            InitializeComponent();
            BuildControls();
        }

        private TextBox txtEmail;
        private TextBox txtPassword;
        private Button btnLogin;
        private Label lblStatus;

        private void BuildControls()
        {
            this.Text = "CondoSphere - Login";
            this.Width = 380;
            this.Height = 240;
            this.StartPosition = FormStartPosition.CenterScreen;

            var lblE = new Label { Text = "Email", Left = 20, Top = 20, Width = 320 };
            txtEmail = new TextBox { Left = 20, Top = 40, Width = 320 };

            var lblP = new Label { Text = "Password", Left = 20, Top = 75, Width = 320 };
            txtPassword = new TextBox { Left = 20, Top = 95, Width = 320, UseSystemPasswordChar = true };

            btnLogin = new Button { Text = "Login", Left = 20, Top = 130, Width = 120 };
            btnLogin.Click += async (_, __) => await DoLoginAsync();

            lblStatus = new Label { Left = 20, Top = 165, Width = 320, ForeColor = System.Drawing.Color.Firebrick };

            this.Controls.AddRange(new Control[] { lblE, txtEmail, lblP, txtPassword, btnLogin, lblStatus });
        }

        private async Task DoLoginAsync()
        {
            try
            {
                lblStatus.Text = "";
                btnLogin.Enabled = false;

                var req = new LoginRequest
                {
                    Email = txtEmail.Text?.Trim(),
                    Password = txtPassword.Text
                };

                // POST /api/auth/login
                var resp = await ApiClient.PostAsync<LoginRequest, LoginResponse>("auth/login", req);

                if (string.IsNullOrWhiteSpace(resp?.Token))
                {
                    lblStatus.Text = "Invalid login credentials.";
                    btnLogin.Enabled = true;
                    return;
                }

                ApiClient.SetToken(resp.Token);

                // Abre a Main
                var main = new MainForm();
                main.FormClosed += (_, __) => this.Close();
                main.Show();
                this.Hide();
            }
            catch (UnauthorizedAccessException)
            {
                lblStatus.Text = "Unauthorized.";
            }
            catch (Exception ex)
            {
                lblStatus.Text = ex.Message;
            }
            finally
            {
                btnLogin.Enabled = true;
            }
        }
    }
}
