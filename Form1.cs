using System;
using System.IO;
using System.Reflection;
using System.Windows.Forms;
using System.Drawing;

namespace WindowsFormsApp
{
    public partial class Form1 : Form
    {
        private NotifyIcon trayIcon;
        private ContextMenuStrip trayMenu;

        public Form1()
        {
            InitializeComponent();
            InitializeTrayIcon();
            this.Text = $"Бэкап лайт v{Assembly.GetExecutingAssembly().GetName().Version}";

            // Замените MyIcon на имя вашей иконки в ресурсах
            using (var stream = new MemoryStream(Properties.Resources.MyIcon))
            {
                this.Icon = new Icon(stream, new Size(16, 16));
            }

            // Загрузка сохраненных путей
            LoadPaths();

            // Существующие обработчики
            btnSelectSource.Click += BtnSelectSource_Click;
            btnSelectDestination.Click += BtnSelectDestination_Click;
            btnBackup.Click += BtnBackup_Click;

            // Добавляем эффекты при наведении для кнопок
            ConfigureButtonHoverEffects(btnSelectSource);
            ConfigureButtonHoverEffects(btnSelectDestination);
            ConfigureButtonHoverEffects(btnBackup);

            // Обработчик сворачивания окна
            this.Resize += MainForm_Resize;
        }

        private void InitializeTrayIcon()
        {
            trayMenu = new ContextMenuStrip();
            trayMenu.Items.Add("Развернуть", null, OnTrayRestore);
            trayMenu.Items.Add("Бэкап", null, (s, e) => BtnBackup_Click(s, e));
            trayMenu.Items.Add("-"); // Разделитель
            trayMenu.Items.Add("О программе", null, OnAbout);
            trayMenu.Items.Add("Выход", null, OnTrayExit);

            trayIcon = new NotifyIcon
            {
                Text = "Бэкап лайт",
                Icon = new Icon(new MemoryStream(Properties.Resources.MyIcon)), // Используем иконку из ресурсов
                ContextMenuStrip = trayMenu,
                Visible = false
            };

            // Убираем обработчик одиночного клика, оставляем только двойной
            trayIcon.MouseClick += (s, e) =>
            {
                if (e.Button == MouseButtons.Left)
                {
                    OnTrayRestore(s, e);
                }
            };
        }

        private void OnAbout(object sender, EventArgs e)
        {
            MessageBox.Show("© 2025 Бэкап лайт. Все права защищены. Разработчик Lucky", "О программе", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void MainForm_Resize(object sender, EventArgs e)
        {
            if (WindowState == FormWindowState.Minimized)
            {
                this.Hide();
                trayIcon.Visible = true;
                trayIcon.BalloonTipTitle = "Бэкап лайт";
                trayIcon.BalloonTipText = "Приложение свернуто в трей";
                trayIcon.BalloonTipIcon = ToolTipIcon.Info;
                trayIcon.ShowBalloonTip(3000); // Увеличиваем время отображения до 3000 миллисекунд
            }
        }

        private void OnTrayRestore(object sender, EventArgs e)
        {
            this.Show();
            this.WindowState = FormWindowState.Normal;
            trayIcon.Visible = false;
        }

        private void OnTrayExit(object sender, EventArgs e)
        {
            trayIcon.Visible = false;
            Application.Exit();
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            if (trayIcon != null)
            {
                trayIcon.Dispose();
            }
            if (trayMenu != null)
            {
                trayMenu.Dispose();
            }
            base.OnFormClosing(e);
        }

        private void ConfigureButtonHoverEffects(Button button)
        {
            button.MouseEnter += (s, e) =>
            {
                button.BackColor = System.Drawing.Color.FromArgb(52, 152, 219);
            };
            button.MouseLeave += (s, e) =>
            {
                button.BackColor = System.Drawing.Color.FromArgb(41, 128, 185);
            };
        }

        private void BtnSelectSource_Click(object sender, EventArgs e)
        {
            using (FolderBrowserDialog folderBrowserDialog = new FolderBrowserDialog())
            {
                if (folderBrowserDialog.ShowDialog() == DialogResult.OK)
                {
                    // Логика для обработки выбранной папки
                    string selectedPath = folderBrowserDialog.SelectedPath;
                    // Например, сохранить путь в текстовое поле
                    txtSourcePath.Text = selectedPath;
                    SavePaths();
                }
            }
        }

        private void BtnSelectDestination_Click(object sender, EventArgs e)
        {
            using (FolderBrowserDialog folderBrowserDialog = new FolderBrowserDialog())
            {
                if (folderBrowserDialog.ShowDialog() == DialogResult.OK)
                {
                    // Логика для обработки выбранной папки
                    string selectedPath = folderBrowserDialog.SelectedPath;
                    // Например, сохранить путь в текстовое поле
                    txtDestinationPath.Text = selectedPath;
                    SavePaths();
                }
            }
        }

        private void BtnBackup_Click(object sender, EventArgs e)
        {
            string sourcePath = txtSourcePath.Text;
            string destinationPath = txtDestinationPath.Text;

            if (Directory.Exists(sourcePath) && Directory.Exists(destinationPath))
            {
                try
                {
                    string backupFolderName = Path.GetFileName(sourcePath) + "_Backup_" + DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");
                    string backupFolderPath = Path.Combine(destinationPath, backupFolderName);

                    Directory.CreateDirectory(backupFolderPath);

                    foreach (string dirPath in Directory.GetDirectories(sourcePath, "*", SearchOption.AllDirectories))
                    {
                        Directory.CreateDirectory(dirPath.Replace(sourcePath, backupFolderPath));
                    }

                    foreach (string newPath in Directory.GetFiles(sourcePath, "*.*", SearchOption.AllDirectories))
                    {
                        File.Copy(newPath, newPath.Replace(sourcePath, backupFolderPath), true);
                    }

                    ShowAutoClosingMessageBox("Резервное копирование успешно завершено!", "Успех", 500);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Произошла ошибка во время резервного копирования: {ex.Message}", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
            else
            {
                MessageBox.Show("Пожалуйста, выберите допустимые пути для исходной и конечной папок.", "Предупреждение", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private void ShowAutoClosingMessageBox(string message, string title, int timeout)
        {
            var timer = new Timer();
            timer.Interval = timeout;
            timer.Tick += (s, e) =>
            {
                var msgBox = (Form)((Timer)s).Tag;
                msgBox.Close();
                timer.Stop();
                timer.Dispose();
            };
            timer.Start();

            var msgForm = new Form()
            {
                Text = title,
                StartPosition = FormStartPosition.CenterScreen,
                Size = new System.Drawing.Size(300, 150),
                FormBorderStyle = FormBorderStyle.FixedDialog,
                BackColor = System.Drawing.Color.FromArgb(41, 128, 185),
                ForeColor = System.Drawing.Color.White
            };
            var label = new Label()
            {
                Text = message,
                Dock = DockStyle.Fill,
                TextAlign = System.Drawing.ContentAlignment.MiddleCenter,
                Font = new System.Drawing.Font("Arial", 12, System.Drawing.FontStyle.Bold),
                ForeColor = System.Drawing.Color.White
            };
            msgForm.Controls.Add(label);
            timer.Tag = msgForm;
            msgForm.ShowDialog();
        }

        private void SavePaths()
        {
            Properties.Settings.Default["SourcePath"] = txtSourcePath.Text;
            Properties.Settings.Default["DestinationPath"] = txtDestinationPath.Text;
            Properties.Settings.Default.Save();
        }

        private void LoadPaths()
        {
            txtSourcePath.Text = Properties.Settings.Default["SourcePath"]?.ToString();
            txtDestinationPath.Text = Properties.Settings.Default["DestinationPath"]?.ToString();
        }
    }
<<<<<<< HEAD
}
=======
}
>>>>>>> 21c8675f2eed0d4c2847e9b8336e4838d5bfb9aa
