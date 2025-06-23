using System;
using System.Threading.Tasks;
using System.Windows;
using WordPopupApp.Services;
using WordPopupApp.Models;
using WordPopupApp.ViewModels;
using WordPopupApp.Views;
using System.Windows.Interop;
using System.Windows.Input;

namespace WordPopupApp
{
    public partial class MainWindow : Window
    {
        private GlobalHotKey _hotKey;
        private readonly DictionaryService _dictionaryService;
        private readonly AnkiService _ankiService;
        private readonly SettingsService _settingsService;
        private AppSettings _currentSettings;

        public MainWindow()
        {
            InitializeComponent();
            _dictionaryService = new DictionaryService();
            _ankiService = new AnkiService();
            _settingsService = new SettingsService();
        }

        private async void Window_Loaded(object sender, RoutedEventArgs e)
        {
            // 加载设置
            // 加载设置
            _currentSettings = _settingsService.LoadSettings();

            // 动态获取 Anki 中现有的 Deck 与 Model
            try
            {
                var deckNames = await _ankiService.GetDeckNamesAsync();
                DeckComboBox.ItemsSource = deckNames;
                DeckComboBox.SelectedItem = deckNames.Contains(_currentSettings.AnkiDeckName)
                    ? _currentSettings.AnkiDeckName
                    : deckNames.FirstOrDefault();

                var modelNames = await _ankiService.GetModelNamesAsync();
                ModelComboBox.ItemsSource = modelNames;
                ModelComboBox.SelectedItem = modelNames.Contains(_currentSettings.AnkiModelName)
                    ? _currentSettings.AnkiModelName
                    : modelNames.FirstOrDefault();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"无法连接到 Anki (请确认已安装并运行 AnkiConnect)：{ex.Message}");
            }

            // 注册全局热键 Ctrl + Z
            try
            {
                uint vk = (uint)KeyInterop.VirtualKeyFromKey(Key.Z);  // 把 WPF 的 Key 转成 VK
                _hotKey = new GlobalHotKey(this, HotKeyModifiers.MOD_CONTROL, vk);
                // Virtual-Key Codes: https://docs.microsoft.com/en-us/windows/win32/inputdev/virtual-key-codes
                _hotKey.HotKeyPressed += OnHotKeyPressed;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"注册热键失败: {ex.Message}");
            }
        }

        private async void OnHotKeyPressed()
        {
            // 1. 模拟Ctrl+C复制选中内容
            GlobalHotKey.SimulateCtrlC();

            // 2. 从剪贴板获取文本
            string selectedText = GlobalHotKey.GetTextFromClipboard()?.Trim();

            if (string.IsNullOrEmpty(selectedText))
            {
                return;
            }

            // 3. 异步查询
            var entry = await _dictionaryService.LookupAsync(selectedText);

            // 4. 创建ViewModel和View
            var viewModel = new PopupResultViewModel(entry, _ankiService, _currentSettings);
            var popup = new PopupResultWindow
            {
                DataContext = viewModel
            };

            // 5. 在鼠标旁显示窗口
            popup.SetPositionAndShow();
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            if (DeckComboBox.SelectedItem == null || ModelComboBox.SelectedItem == null)
            {
                MessageBox.Show("请选择有效的牌组和笔记类型！");
                return;
            }
            _currentSettings.AnkiDeckName = DeckComboBox.SelectedItem.ToString();
            _currentSettings.AnkiModelName = ModelComboBox.SelectedItem.ToString();
            
            _settingsService.SaveSettings(_currentSettings);
            MessageBox.Show("设置已保存！");
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            _hotKey?.Dispose();
        }
    }
}