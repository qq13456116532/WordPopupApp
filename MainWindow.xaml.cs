using System;
using System.Threading.Tasks;
using System.Windows;
using WordPopupApp.Services;
using WordPopupApp.Models;
using WordPopupApp.ViewModels;
using WordPopupApp.Views;
using System.Windows.Interop;
using System.Windows.Input;
using System.Net.Http;
using Newtonsoft.Json;
using System.Text;
using Newtonsoft.Json.Linq;

namespace WordPopupApp
{
    public partial class MainWindow : Window
    {
        private GlobalHotKey _hotKey;
        private PopupResultWindow _popup;            // 单例实例

        private PopupResultWindow Popup
        => _popup ??= new PopupResultWindow      // 第一次用时才创建
        {
            Owner   = this,                      // 可选：让浮窗跟随主窗关闭
            WindowStartupLocation = WindowStartupLocation.Manual
        };

        private readonly DictionaryService _dictionaryService;
        private readonly TranslationService _translationService; // <-- 新增
        private readonly AnkiService _ankiService;
        private readonly SettingsService _settingsService;
        private AppSettings _currentSettings;

        public MainWindow()
        {
            InitializeComponent();
            _dictionaryService = new DictionaryService();
            _translationService = new TranslationService(); // <-- 新增：实例化翻译服务
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
                _hotKey.HotKeyPressed += async () => await HandleHotKeyAsync();

            }
            catch (Exception ex)
            {
                MessageBox.Show($"注册热键失败: {ex.Message}");
            }
        }

        // 热键回调（已改成 async Task）
        private async Task HandleHotKeyAsync()
        {
            try
            {
                GlobalHotKey.SimulateCtrlC();
                var text = GlobalHotKey.GetTextFromClipboard()?.Trim();
                if (string.IsNullOrWhiteSpace(text)) return;

                var dictionaryTask = _dictionaryService.LookupAsync(text);
                var translationTask = _translationService.TranslateToChineseAsync(text);

                await Task.WhenAll(dictionaryTask, translationTask);

                var entry = await dictionaryTask;
                var chineseTranslation = await translationTask;

                // [修改] 创建ViewModel时，传入关闭窗口的回调
                Popup.DataContext = new PopupResultViewModel(
                                        entry,
                                        chineseTranslation,
                                        _ankiService,
                                        _currentSettings,
                                        this,
                                        () => Popup.Hide()); // <--- 新增此行

                var p = System.Windows.Forms.Cursor.Position;
                Popup.Left = p.X + 20;
                Popup.Top = p.Y + 20;
                Popup.Show();
                Popup.Activate();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"热键处理失败：{ex.Message}");
            }
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            if (DeckComboBox.SelectedItem == null)
            {
                MessageBox.Show("请选择有效的牌组和笔记类型！");
                return;
            }
            _currentSettings.AnkiDeckName = DeckComboBox.SelectedItem.ToString();

            _settingsService.SaveSettings(_currentSettings);
            MessageBox.Show("设置已保存！");
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            _hotKey?.Dispose();
        }

        public async Task EnsureWordPopUpNoteModelAsync()
        {
            const string ModelName = "WordPopUpNote";

            string[] Fields = { "单词", "释义", "笔记", "例句" };

            string FrontTemplate = @"
                <div class=""bar head"">牌组名称 : {{Deck}}</div>
                <div class=""section"">
                <div class=""expression"">{{单词}}</div>
                </div>
                ";

            string BackTemplate = @"
                <div class=""bar head"">牌组名称 : {{Deck}}</div>
                <div class=""section"">
                <div class=""expression1"">{{单词}}</div>
                <div id=""definition"" class=""items"">{{释义}}</div>
                </div>
                <div class=""section"">
                {{#笔记}}
                    <div class=""bar back"">词组详解</div>
                    <div id=""notes"" class=""items"">{{笔记}}</div>
                {{/笔记}}
                {{#例句}}
                    <div class=""bar head1"">词组例句</div>
                    <div id=""sentence"" class=""items"">{{例句}}</div>
                {{/例句}}
                </div>
                <div class=""bar foot"">
                <div id=""url"">数据源:《英语常用短语词典》</div>
                </div>
                <script type=""text/javascript"">
                var colorMap = {
                    'n.':'#e3412f',
                    'a.':'#f8b002',
                    'adj.':'#f8b002',
                    'ad.':'#684b9d',
                    'adv.':'#684b9d',
                    'v.':'#539007',
                    'vi.':'#539007',
                    'vt.':'#539007',
                    'prep.':'#04B7C9',
                    'conj.':'#04B7C9',
                    'pron.':'#04B7C9',
                    'art.':'#04B7C9',
                    'num.':'#04B7C9',
                    'int.':'#04B7C9',
                    'interj.':'#04B7C9',
                    'modal.':'#04B7C9',
                    'aux.':'#04B7C9',
                    'pl.':'#D111D3',
                    'abbr.':'#D111D3',
                };
                [].forEach.call(document.querySelectorAll('#definition'), function(div) {
                div.innerHTML = div.innerHTML
                .replace(/\b[a-z]+\./g, function(symbol) {
                    if(colorMap[symbol]) {
                    return '<a style=""background-color:' + colorMap[symbol] + '"">' +
                    symbol + '</a>';
                    }else{
                    return symbol;
                    }
                });
                });
                </script>
                ";

            string Css = @"
                <!-- 英语划词助手模板 -->
                <style>
                .card {
                font-family:微软雅黑;
                font-size: 14px;
                text-align: left;
                color:#1d2129;
                background-color:#e9ebee;
                }
                .bar{
                border-radius: 3px;
                border-bottom: 1px solid #29487d;
                color: #fff;
                padding: 5px;
                background-position : 5px center;
                text-decoration:none;
                font-size: 12px;
                color: #fff;
                font-weight: bold;
                }
                .head{
                padding-left:30px;
                background: #365899 url(_clipboard.png) no-repeat;
                background-position : 5px center;
                }
                .head1{
                padding-left:30px;
                margin-left:0px;
                background: #365899 url(_pencil.png) no-repeat;
                background-position : 5px center;
                }
                .back{
                padding-left:30px;
                margin-left:0px;
                background: #365899 url(_bulb.png) no-repeat;
                background-position : 5px center;
                }
                .foot{
                padding-right:25px;
                text-align:right;
                background: #365899 url(_cloud.png) no-repeat right;
                }
                .section {
                border: 1px solid;
                border-color: #e5e6e9 #dfe0e4 #d0d1d5;
                border-radius: 3px;
                background-color: #fff;
                position: relative;
                margin: 5px 0;
                }
                .expression{
                font-size: 35px;
                margin: 0 12px;
                padding: 20px 0 8px 0;
                border-bottom: 0px solid #e5e5e5;
                }
                .expression1{
                font-size: 35px;
                margin: 0 12px;
                padding: 20px 0 8px 0;
                border-bottom: 1px solid #e5e5e5;
                }
                .phonetic{
                font-size:24px;
                margin: 0px 12px 0px 0px;
                padding: 10px 0px 8px 0px;
                }
                .items{
                border-top: 1px solid #e5e5e5;
                line-height:1.5em;
                font-size: 18px;
                margin: 0 12px;
                padding: 10px 0 8px 0;
                }
                #definition{ line-height:1.5em;
                font-size:20px;
                border-top: 0px;
                }
                #url a{
                text-decoration:none;
                font-size: 12px;
                color: #fff;
                font-weight: bold;
                }
                #definition a {
                text-decoration: none;
                padding: 1px 6px 2px 5px;
                margin: 0 5px 0 0;
                font-size: 12px;
                color: white;
                font-weight: normal;
                border-radius: 4px
                }
                #definition a.pos_n {
                    background-color: #e3412f
                }
                #definition a.pos_v {
                    background-color: #539007
                }
                #definition a.pos_a {
                    background-color: #f8b002
                }
                #definition a.pos_r {
                    background-color: #684b9d
                }
                #sentence b{
                font-weight:      normal;
                border-radius:    3px;
                color:            #fff;
                background-color: #666;
                padding-left:     3px;
                padding-right:    3px;
                }
                .voice img{
                margin-left:5px;
                width: 24px;
                height: 24px;
                }
                </style>
                <script>
                function toggle(e){
                    var box=document.getElementById(e);
                    if(box.style.display=='none'){
                        box.style.display='block';
                    }
                    else{
                        box.style.display='none';
                    }
                }
                </script>
                ";



            // 1. 检查模型是否存在
            var existModels = await AnkiConnectRequestAsync<List<string>>("modelNames");
            if (existModels.Contains(ModelName))
            {
                // 2. 如果存在，不确定怎么做，先空着 TO DO
                return;
            }
            // 3. 新建 Note Type
            await AnkiConnectRequestAsync<object>("createModel", new
            {
                modelName = ModelName,
                inOrderFields = Fields,
                css = Css,
                cardTemplates = new[]
                {
                    new {
                        Name = "WordPopUpCard",
                        Front = FrontTemplate,
                        Back = BackTemplate
                    }
                }
            });
        }
        public async Task<T> AnkiConnectRequestAsync<T>(string action, object parameters = null)
        {
            var client = new HttpClient();
            var postObj = new
            {
                action,
                version = 6,
                @params = parameters ?? new { }
            };
            var resp = await client.PostAsync("http://127.0.0.1:8765", 
                new StringContent(JsonConvert.SerializeObject(postObj), Encoding.UTF8, "application/json"));
            var json = await resp.Content.ReadAsStringAsync();
            var root = JObject.Parse(json);
            if (root["error"]?.Type != JTokenType.Null)
                throw new Exception(root["error"].ToString());
            return root["result"].ToObject<T>();
        }


    }
}