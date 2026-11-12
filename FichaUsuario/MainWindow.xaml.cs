using System;
using System.Data;
using System.Data.SQLite;
using System.Net.Mail;
using System.Windows;
using System.Windows.Controls;

namespace FichaUsuario
{
    public partial class MainWindow : Window
    {
        private const string DbPath = "fichas.db";
        private const string ConnectionString = "Data Source=fichas.db;Version=3;";

        public MainWindow()
        {
            InitializeComponent();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            InitializeDatabase();
            LoadCities();
            dpFecha.SelectedDate = DateTime.Today;
        }

        private void InitializeDatabase()
        {
            using var connection = new SQLiteConnection(ConnectionString);
            connection.Open();

            var createCiudadTable = @"
                CREATE TABLE IF NOT EXISTS Ciudad (
                    Id     INTEGER PRIMARY KEY AUTOINCREMENT,
                    Nombre TEXT NOT NULL UNIQUE
                );";

            var createUsuarioTable = @"
                CREATE TABLE IF NOT EXISTS Usuario (
                    Id        INTEGER PRIMARY KEY AUTOINCREMENT,
                    Nombre    TEXT    NOT NULL,
                    Correo    TEXT    NOT NULL UNIQUE,
                    CiudadId  INTEGER NOT NULL REFERENCES Ciudad(Id),
                    Fecha     TEXT    NOT NULL,
                    Acepta    INTEGER NOT NULL CHECK (Acepta = 1),
                    FechaAlta TEXT    NOT NULL DEFAULT (datetime('now','localtime'))
                );";

            var insertCities = @"
                INSERT OR IGNORE INTO Ciudad (Nombre) VALUES
                    ('Almería'), ('Cádiz'), ('Córdoba'), ('Granada'),
                    ('Huelva'), ('Jaén'), ('Málaga'), ('Sevilla');";

            using var cmd = connection.CreateCommand();
            cmd.CommandText = createCiudadTable;
            cmd.ExecuteNonQuery();

            cmd.CommandText = createUsuarioTable;
            cmd.ExecuteNonQuery();

            cmd.CommandText = insertCities;
            cmd.ExecuteNonQuery();
        }

        private void LoadCities()
        {
            using var connection = new SQLiteConnection(ConnectionString);
            connection.Open();

            var query = "SELECT Id, Nombre FROM Ciudad ORDER BY Nombre;";

            using var cmd = new SQLiteCommand(query, connection);
            using var reader = cmd.ExecuteReader();

            var dt = new DataTable();
            dt.Load(reader);

            cmbCiudad.ItemsSource = dt.DefaultView;
        }

        private void chkCondiciones_Checked(object sender, RoutedEventArgs e)
        {
            btnGuardar.IsEnabled = true;
        }

        private void chkCondiciones_Unchecked(object sender, RoutedEventArgs e)
        {
            btnGuardar.IsEnabled = false;
        }

        private void btnGuardar_Click(object sender, RoutedEventArgs e)
        {
            if (!ValidateForm())
                return;

            try
            {
                SaveUser();
                ShowSummary();
                ClearForm();
            }
            catch (SQLiteException ex) when (ex.ErrorCode == 19)
            {
                MessageBox.Show($"Ya existe un usuario con el correo {txtCorreo.Text.Trim()}.",
                    "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                txtCorreo.Focus();
            }
            catch (Exception)
            {
                MessageBox.Show("Error al guardar en la base de datos.",
                    "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private bool ValidateForm()
        {
            var nombre = txtNombre.Text.Trim();
            if (string.IsNullOrWhiteSpace(nombre))
            {
                MessageBox.Show("Escribe el nombre del usuario.", "Validación",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                txtNombre.Focus();
                return false;
            }

            var correo = txtCorreo.Text.Trim();
            if (string.IsNullOrWhiteSpace(correo) || !IsValidEmail(correo))
            {
                MessageBox.Show("El correo no tiene un formato válido (ejemplo: nombre@dominio.es).",
                    "Validación", MessageBoxButton.OK, MessageBoxImage.Warning);
                txtCorreo.Focus();
                return false;
            }

            if (cmbCiudad.SelectedValue == null)
            {
                MessageBox.Show("Selecciona una ciudad.", "Validación",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                cmbCiudad.Focus();
                return false;
            }

            if (dpFecha.SelectedDate == null)
            {
                MessageBox.Show("Selecciona una fecha.", "Validación",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                dpFecha.Focus();
                return false;
            }

            return true;
        }

        private bool IsValidEmail(string email)
        {
            return System.Text.RegularExpressions.Regex.IsMatch(email,
                @"^[^@\s]+@[^@\s]+\.[a-zA-Z]{2,}$");
        }

        private void SaveUser()
        {
            using var connection = new SQLiteConnection(ConnectionString);
            connection.Open();

            var query = @"INSERT INTO Usuario (Nombre, Correo, CiudadId, Fecha, Acepta)
                          VALUES (@Nombre, @Correo, @CiudadId, @Fecha, @Acepta);";

            using var cmd = new SQLiteCommand(query, connection);
            cmd.Parameters.AddWithValue("@Nombre", txtNombre.Text.Trim());
            cmd.Parameters.AddWithValue("@Correo", txtCorreo.Text.Trim());
            cmd.Parameters.AddWithValue("@CiudadId", cmbCiudad.SelectedValue);
            cmd.Parameters.AddWithValue("@Fecha", dpFecha.SelectedDate.Value.ToString("yyyy-MM-dd"));
            cmd.Parameters.AddWithValue("@Acepta", 1);

            cmd.ExecuteNonQuery();
        }

        private void ShowSummary()
        {
            var nombre = txtNombre.Text.Trim();
            var correo = txtCorreo.Text.Trim();
            var ciudad = ((DataRowView)cmbCiudad.SelectedItem)["Nombre"].ToString();
            var fecha = dpFecha.SelectedDate.Value.ToString("dd/MM/yyyy");

            txtResumen.Text = $"{nombre} — {correo} — {ciudad} — {fecha}";
        }

        private void ClearForm()
        {
            txtNombre.Clear();
            txtCorreo.Clear();
            cmbCiudad.SelectedIndex = -1;
            dpFecha.SelectedDate = DateTime.Today;
            chkCondiciones.IsChecked = false;
            btnGuardar.IsEnabled = false;
            txtNombre.Focus();
        }
    }
}