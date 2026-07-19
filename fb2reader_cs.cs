// fb2reader_cs.cs — Читалка FB2 с анимацией страниц на C# (WPF)

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Xml;

namespace FB2ReaderWPF
{
    public partial class MainWindow : Window
    {
        private List<string> pages = new List<string>();
        private int currentPage = 0;
        private bool nightMode = false;
        private Dictionary<string, int> bookmarks = new Dictionary<string, int>();
        private int fontSize = 14;
        private string stateFile = "reader_state.json";

        private TextBlock textBlock;
        private Label statusLabel, infoLabel;
        private ScrollViewer scrollViewer;

        public MainWindow()
        {
            InitializeComponent();
            CreateUI();
            LoadState();
            ApplyTheme();
        }

        private void CreateUI()
        {
            Title = "📖 FB2Reader — C#";
            Width = 1000;
            Height = 700;
            var grid = new Grid();
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

            var toolbar = new StackPanel { Orientation = Orientation.Horizontal };
            var openBtn = new Button { Content = "Открыть", Width = 80 };
            var bookmarkBtn = new Button { Content = "Закладка", Width = 80 };
            var gotoBtn = new Button { Content = "Перейти к закладке", Width = 100 };
            var nightBtn = new Button { Content = "Ночной режим", Width = 100 };
            var incBtn = new Button { Content = "A+", Width = 40 };
            var decBtn = new Button { Content = "A-", Width = 40 };
            toolbar.Children.Add(openBtn);
            toolbar.Children.Add(bookmarkBtn);
            toolbar.Children.Add(gotoBtn);
            toolbar.Children.Add(nightBtn);
            toolbar.Children.Add(incBtn);
            toolbar.Children.Add(decBtn);
            Grid.SetRow(toolbar, 0);
            grid.Children.Add(toolbar);

            infoLabel = new Label { Content = "Книга не загружена" };
            Grid.SetRow(infoLabel, 1);
            grid.Children.Add(infoLabel);

            scrollViewer = new ScrollViewer();
            textBlock = new TextBlock { TextWrapping = TextWrapping.Wrap, FontSize = fontSize };
            scrollViewer.Content = textBlock;
            Grid.SetRow(scrollViewer, 2);
            grid.Children.Add(scrollViewer);

            statusLabel = new Label { Content = "Готов" };
            Grid.SetRow(statusLabel, 3);
            grid.Children.Add(statusLabel);

            Content = grid;

            openBtn.Click += (s, e) => OpenBook();
            bookmarkBtn.Click += (s, e) => AddBookmark();
            gotoBtn.Click += (s, e) => GotoBookmark();
            nightBtn.Click += (s, e) => ToggleNight();
            incBtn.Click += (s, e) => { fontSize += 2; textBlock.FontSize = fontSize; };
            decBtn.Click += (s, e) => { if (fontSize > 8) { fontSize -= 2; textBlock.FontSize = fontSize; } };

            this.KeyDown += (s, e) => {
                if (e.Key == Key.Right || e.Key == Key.Space) NextPage();
                if (e.Key == Key.Left) PrevPage();
                if (e.Key == Key.O && Keyboard.Modifiers == ModifierKeys.Control) OpenBook();
            };
        }

        private void OpenBook()
        {
            var dialog = new Microsoft.Win32.OpenFileDialog { Filter = "FB2 (*.fb2)|*.fb2" };
            if (dialog.ShowDialog() == true)
            {
                try
                {
                    var doc = new XmlDocument();
                    doc.Load(dialog.FileName);
                    var ns = new XmlNamespaceManager(doc.NameTable);
                    ns.AddNamespace("fb", "http://www.gribuser.ru/xml/fictionbook/2.0");
                    var nodes = doc.SelectNodes("//fb:p", ns);
                    var text = new StringBuilder();
                    foreach (XmlNode node in nodes)
                        text.Append(node.InnerText).Append(" ");
                    string fullText = System.Text.RegularExpressions.Regex.Replace(text.ToString(), @"\s+", " ").Trim();
                    pages.Clear();
                    int pageSize = 2000;
                    for (int i = 0; i < fullText.Length; i += pageSize)
                        pages.Add(fullText.Substring(i, Math.Min(pageSize, fullText.Length - i)));
                    currentPage = 0;
                    ShowPage(currentPage);
                    infoLabel.Content = "Книга: " + System.IO.Path.GetFileName(dialog.FileName);
                    statusLabel.Content = $"Загружено, страниц: {pages.Count}";
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Ошибка открытия FB2: " + ex.Message);
                }
            }
        }

        private void ShowPage(int idx)
        {
            if (pages.Count == 0 || idx < 0 || idx >= pages.Count) return;
            currentPage = idx;
            // Анимация: затухание и появление
            DoubleAnimation fadeOut = new DoubleAnimation(1, 0, TimeSpan.FromSeconds(0.2));
            DoubleAnimation fadeIn = new DoubleAnimation(0, 1, TimeSpan.FromSeconds(0.2));
            var storyboard = new Storyboard();
            Storyboard.SetTarget(fadeOut, textBlock);
            Storyboard.SetTargetProperty(fadeOut, new PropertyPath(UIElement.OpacityProperty));
            Storyboard.SetTarget(fadeIn, textBlock);
            Storyboard.SetTargetProperty(fadeIn, new PropertyPath(UIElement.OpacityProperty));
            storyboard.Children.Add(fadeOut);
            storyboard.Children.Add(fadeIn);
            fadeOut.Completed += (s, e) => {
                textBlock.Text = pages[idx];
            };
            storyboard.Begin();
            UpdateStatus();
        }

        private void NextPage()
        {
            if (currentPage < pages.Count-1) ShowPage(currentPage+1);
        }

        private void PrevPage()
        {
            if (currentPage > 0) ShowPage(currentPage-1);
        }

        private void ToggleNight()
        {
            nightMode = !nightMode;
            ApplyTheme();
            statusLabel.Content = "Ночной режим " + (nightMode ? "включён" : "выключен");
        }

        private void ApplyTheme()
        {
            var bg = nightMode ? new SolidColorBrush(Color.FromRgb(30,30,30)) : new SolidColorBrush(Colors.White);
            var fg = nightMode ? new SolidColorBrush(Color.FromRgb(212,212,212)) : new SolidColorBrush(Colors.Black);
            textBlock.Background = bg;
            textBlock.Foreground = fg;
            scrollViewer.Background = bg;
        }

        private void AddBookmark()
        {
            if (pages.Count == 0) return;
            string name = Microsoft.VisualBasic.Interaction.InputBox("Введите название закладки:", "Закладка", "", -1, -1);
            if (!string.IsNullOrEmpty(name))
            {
                bookmarks[name] = currentPage;
                statusLabel.Content = "Закладка добавлена: " + name;
            }
        }

        private void GotoBookmark()
        {
            if (bookmarks.Count == 0) { MessageBox.Show("Нет закладок"); return; }
            var names = bookmarks.Keys.ToArray();
            string selected = (string)Microsoft.VisualBasic.Interaction.InputBox("Выберите закладку:", "Перейти к закладке", names[0], -1, -1);
            if (!string.IsNullOrEmpty(selected) && bookmarks.ContainsKey(selected))
            {
                ShowPage(bookmarks[selected]);
                statusLabel.Content = "Переход к закладке: " + selected;
            }
        }

        private void UpdateStatus()
        {
            if (pages.Count == 0) return;
            int total = pages.Count;
            int percent = (currentPage+1)*100/total;
            statusLabel.Content = $"Страница {currentPage+1}/{total} ({percent}%)";
        }

        private void LoadState()
        {
            if (System.IO.File.Exists(stateFile))
            {
                string json = System.IO.File.ReadAllText(stateFile);
                if (json.Contains("nightMode\":true")) nightMode = true;
                int idx = json.IndexOf("fontSize\":");
                if (idx != -1)
                {
                    int start = idx + 10;
                    int end = json.IndexOf(",", start);
                    if (end == -1) end = json.IndexOf("}", start);
                    int.TryParse(json.Substring(start, end-start).Trim(), out fontSize);
                }
            }
        }

        private void SaveState()
        {
            System.IO.File.WriteAllText(stateFile, $"{{\"nightMode\":{nightMode.ToString().ToLower()},\"fontSize\":{fontSize}}}");
        }

        [STAThread]
        static void Main()
        {
            var app = new Application();
            app.Run(new MainWindow());
        }
    }
}
