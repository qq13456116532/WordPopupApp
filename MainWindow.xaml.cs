using System;
using System.Threading.Tasks;
using System.Threading;
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
        private PopupResultWindow _popup;

        private PopupResultWindow Popup
        => _popup ??= new PopupResultWindow
        {
            WindowStartupLocation = WindowStartupLocation.Manual
        };

        // [删除] 旧的服务
        // private readonly DictionaryService _dictionaryService;
        // private readonly TranslationService _translationService; 
        // private readonly PhraseService _phraseService;

        // [新增] 新的服务
        private readonly YoudaoScraperService _youdaoScraperService;
        private readonly AnkiService _ankiService;
        private readonly SettingsService _settingsService;
        private AppSettings _currentSettings;

        public MainWindow()
        {
            InitializeComponent();
            // [修改] 实例化新的服务
            _youdaoScraperService = new YoudaoScraperService();
            _ankiService = new AnkiService();
            _settingsService = new SettingsService();
            _currentSettings = _settingsService.LoadSettings();
        }

        private async void Window_Loaded(object sender, RoutedEventArgs e)
        {
            _currentSettings = _settingsService.LoadSettings();

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

            try
            {
                uint vk = (uint)KeyInterop.VirtualKeyFromKey(Key.Z);
                _hotKey = new GlobalHotKey(this, HotKeyModifiers.MOD_CONTROL, vk);
                _hotKey.HotKeyPressed += async () => await HandleHotKeyAsync();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"注册热键失败: {ex.Message}");
            }
        }

        private CancellationTokenSource _cts;

        private async Task HandleHotKeyAsync()
        {
            _cts?.Cancel();
            _cts = new CancellationTokenSource();
            var cancellationToken = _cts.Token;

            try
            {
                var p = System.Windows.Forms.Cursor.Position;
                GlobalHotKey.SimulateCtrlC();
                var text = GlobalHotKey.GetTextFromClipboard()?.Trim();
                if (string.IsNullOrWhiteSpace(text)) return;

                // [修改] 核心逻辑变更: 只调用一个服务
                var wordCardTask = _youdaoScraperService.ScrapeWordAsync(text, cancellationToken);

                // 显示加载中的弹窗
                Popup.DataContext = new PopupResultViewModel(null, _ankiService, _currentSettings, this, () => Popup.Hide());
                Popup.Left = p.X + 15;
                Popup.Top = p.Y + 15;
                Popup.SetPositionAndShow();

                var wordCard = await wordCardTask;

                cancellationToken.ThrowIfCancellationRequested();

                // [修改] 使用新的数据模型创建 ViewModel
                Popup.DataContext = new PopupResultViewModel(
                                        wordCard,
                                        _ankiService,
                                        _currentSettings,
                                        this,
                                        () => Popup.Hide());

                // 再次显示和激活，确保数据更新后窗口在最前
                Popup.SetPositionAndShow();
            }
            catch (OperationCanceledException)
            {
                // 忽略任务取消异常
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
                MessageBox.Show("请选择有效的牌组！");
                return;
            }
            _currentSettings.AnkiDeckName = DeckComboBox.SelectedItem.ToString() ?? string.Empty;

            _settingsService.SaveSettings(_currentSettings);
            MessageBox.Show("设置已保存！");
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            _hotKey?.Dispose();
            if (_popup != null)
            {
                Application.Current.Shutdown();
            }
        }

        // EnsureWordPopUpNoteModelAsync 和 AnkiConnectRequestAsync 方法保持不变
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
                <div id=""url"">数据源:《有道词典》</div>
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
                    'n/a':'#808080',
                };
                [].forEach.call(document.querySelectorAll('#definition'), function(div) {
                div.innerHTML = div.innerHTML.replace(
                /\b([a-z]+\.)|n\/a\b/gi,      
                function(symbol) {
                    symbol = symbol.toLowerCase(); 
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

            var existModels = await AnkiConnectRequestAsync<List<string>>("modelNames");
            if (existModels.Contains(ModelName)) return;

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