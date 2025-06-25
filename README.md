
---

# WordPopupApp - 划词翻译与Anki制卡助手
---
`WordPopupApp` 是一款为 Windows 用户设计的 C#/.NET 桌面应用。它提供了一个简单高效的划词翻译体验，并能一键将查询的单词及其中英文释义、例句、发音等信息制成卡片，添加到 [Anki](https://apps.ankiweb.net/) 中，极大地提升了阅读和学习效率。

![应用截图](Assets\截图0.png) 
![应用截图](Assets\截图1.png) 

当点击 `+Anki` 之后，在Anki生成的卡片如下：
![应用截图](Assets\截图2.png) 
![应用截图](Assets\截图3.png) 


## ✨ 主要功能

- **全局热键**：在任何应用程序中，选中英文单词或短语，按下 `Ctrl + Z` 即可触发查询。
- **多源查询**：
    - **英文释义**：从 [Free Dictionary API](https://dictionaryapi.dev/) 获取详细的英文定义、词性及例句。
    - **中文翻译**：快速获取单词或短语的中文意思。
    - **相关词组**：通过 [WordsAPI](https://www.wordsapi.com/) 获取与查询单词相关的常用短语。
- **音频播放**：点击发音按钮，即可播放单词的真人发音。
- **一键添加至Anki**：
    - 将单词、音标、中文释义、英文例句、相关词组和发音一键添加到 Anki。
    - 自动在 Anki 中创建名为 `WordPopUpNote` 的精美笔记模板，无需手动配置。
- **高度可配置**：
    - 启动后可在设置界面选择要添加到的 Anki 牌组。
    - 所有配置（如牌组名称、API密钥）都保存在本地 `settings.json` 文件中。

## ⚙️ 先决条件

在使用本应用前，请确保您的电脑满足以下条件：

1.  **操作系统**：Windows。
2.  **.NET 运行库**：需要安装 [.NET 9 Desktop Runtime](https://dotnet.microsoft.com/en-us/download/dotnet/9.0) 或更高版本。
3.  **Anki**：已安装 Anki 桌面客户端。
4.  **AnkiConnect**：在 Anki 中已安装并启用了 [AnkiConnect](https://ankiweb.net/shared/info/2055492159) 插件。**这是与Anki通信的必要组件。**
5.  **WordsAPI 密钥 (可选)**：为了使用“相关词组”功能，您需要一个 WordsAPI 的密钥。
    -   前往 [RapidAPI](https://rapidapi.com/hub) 网站。
    -   搜索并订阅 `WordsAPI` 服务 (通常有免费额度)。
    -   获取您的 `X-RapidAPI-Key`。

## 🚀 安装与设置

1.  **下载应用**：从本项目的 [Releases](https://github.com/qq13456116532/WordPopupApp/releases) 页面下载最新的压缩包，并解压到任意位置。
2.  **配置API密钥 (可选)**：
    -   在解压后的文件夹中，找到 `settings.json` 文件。
    -   用记事本或任何文本编辑器打开它。
    -   将您从 RapidAPI 获取的密钥粘贴到 `WordsApiKey` 字段的值中，并保存文件。
    ```json
    {
      "AnkiDeckName": "Myself",
      "WordsApiKey": "在此处粘贴你从RapidAPI获取的密钥"
    }
    ```
    *如果留空，"相关词组"功能将不可用，但不影响其他核心功能。*
3.  **运行Anki**：确保 Anki 桌面程序正在运行，并且 AnkiConnect 插件已启用。

## 📖 使用方法

1.  **启动应用**：双击运行 `WordPopupApp.exe`。
2.  **配置设置**：
    -   应用启动后会显示一个设置窗口。
    -   程序会自动获取您 Anki 中的所有牌组，请在下拉列表中选择一个您希望用于存词的默认牌组。
    -   点击 “保存设置” 按钮。
3.  **最小化窗口**：将设置窗口最小化，应用会在后台持续运行。
4.  **开始使用**：
    -   在浏览器、PDF阅读器、文档等任何地方，用鼠标选中一个您不认识的英文单词。
    -   按下快捷键 `Ctrl + Z`。
    -   一个查询结果弹窗会立刻出现在您的鼠标旁边。
    -   查看释义，点击 `+ Anki` 按钮即可将该词条添加到您之前设置好的牌组中。
    -   弹窗会在您点击其他地方后自动消失。

## 🏗️ 从源码构建

如果您想自行编译或修改代码，请按以下步骤操作：

1.  克隆本仓库：
    ```bash
    git clone https://github.com/qq13456116532/WordPopupApp.git
    ```
2.  使用 Visual Studio 2022+ 或安装了 C# Dev Kit 插件的 VS Code 打开项目。
3.  还原 NuGet 包依赖。
4.  直接生成 (Build) 或运行 (Run) 项目即可。

## 📦 依赖项

### 外部服务
-   [AnkiConnect](https://ankiweb.net/shared/info/2055492159)
-   [Free Dictionary API](https://dictionaryapi.dev/)
-   [WordsAPI on RapidAPI](https://rapidapi.com/dpventures/api/wordsapi)
-   Google Translate (Unofficial API)

### NuGet 包
-   [CommunityToolkit.Mvvm](https://www.nuget.org/packages/CommunityToolkit.Mvvm/): 用于实现 MVVM 设计模式。
-   [Newtonsoft.Json](https://www.nuget.org/packages/Newtonsoft.Json/): 用于处理 JSON 序列化与反序列化。
-   `Microsoft.WindowsDesktop.App.WindowsForms`: 用于模拟键盘输入和获取鼠标位置。