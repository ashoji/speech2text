# Speech to Text with AI Analysis

Azure Speech to TextとAzure OpenAIを使用した音声文字起こし・AI分析ツール

## 概要

このツールは以下の機能を提供します：
1. Azure AI Service Speech to Textを使用した音声ファイルの文字起こし
   - タイムスタンプ付き出力
   - 話者認識機能（対応音声形式の場合）
2. Azure OpenAIを使用したコールセンター通話内容の分析
3. 要約、感情分析、次のアクションの提案

## 前提条件

1. .NET 8.0 SDK
2. Azure Speech Serviceのサブスクリプション
3. Azure OpenAIのサブスクリプション

## セットアップ

### 1. Azureリソースの準備

#### Speech Service
1. Azure PortalでSpeech Serviceリソースを作成
2. キーとリージョンを取得

#### Azure OpenAI
1. Azure PortalでAzure OpenAIリソースを作成
2. モデル（例：gpt-4.1）をデプロイ
3. エンドポイント、APIキー、デプロイメント名を取得

### 2. 設定ファイルの作成

1. `appsettings.template.json`を`appsettings.json`にコピーします：
```powershell
Copy-Item appsettings.template.json appsettings.json
```

2. `appsettings.json`ファイルの以下の値を実際の値に更新してください：

```json
{
  "Azure": {
    "SpeechService": {
      "SubscriptionKey": "YOUR_SPEECH_SERVICE_KEY",
      "Region": "YOUR_REGION"
    },
    "OpenAI": {
      "Endpoint": "https://YOUR_OPENAI_ENDPOINT.openai.azure.com",
      "ApiKey": "YOUR_OPENAI_API_KEY",
      "DeploymentName": "YOUR_DEPLOYMENT_NAME"
    }
  },
  "AI": {
    "Prompts": {
      "SystemPrompt": "あなたはコールセンターでの顧客対応を分析する専門アシスタントです。...",
      "UserPromptTemplate": "あなたは、コールセンターのオペレーターです。以下の顧客からの問い合わせ通話内容を分析してください：\n\n{0}"
    }
  }
}
```

**注意**: 
- Endpointは、ベースURLのみを指定してください（`/openai/deployments/...`は含めない）
- DeploymentNameは、Azure OpenAIでデプロイしたモデルの名前を指定してください
- `appsettings.json`は機密情報を含むため、Gitにコミットされません
- SystemPromptとUserPromptTemplateは、AI分析の動作をカスタマイズできます

### 3. ビルドと実行

#### 開発環境での実行

```powershell
# プロジェクトをビルド
dotnet build

# 実行
dotnet run "<音声ファイルパス>"
```

#### 本番環境での実行（推奨）

```powershell
# 本番用にビルドして発行（ランタイム含む、推奨）
dotnet publish speech2text.csproj -c Release -r win-x64 --self-contained true -o ./publish

# 本番用にビルドして発行（軽量版、.NET 8.0 Runtime必要）
dotnet publish speech2text.csproj -c Release -r win-x64 --self-contained false -o ./publish-lightweight

# 発行されたexeファイルを実行
./publish/speech2text.exe "<音声ファイルパス>"
```

#### 配布用ZIPファイルの作成

```powershell
# publishフォルダを作成
New-Item -ItemType Directory -Force -Path ./publish

# ランタイム含む版のZIPを作成
dotnet publish speech2text.csproj -c Release -r win-x64 --self-contained true -o ./publish/standalone
# 実際のappsettings.jsonではなく、テンプレートをappsettings.jsonとしてコピー
Copy-Item appsettings.template.json ./publish/standalone/appsettings.json -Force
Copy-Item README.md ./publish/standalone/
Compress-Archive -Path ./publish/standalone/* -DestinationPath ./publish/speech2text-standalone.zip

# 軽量版のZIPを作成
dotnet publish speech2text.csproj -c Release -r win-x64 --self-contained false -o ./publish/lightweight
# 実際のappsettings.jsonではなく、テンプレートをappsettings.jsonとしてコピー
Copy-Item appsettings.template.json ./publish/lightweight/appsettings.json -Force
Copy-Item README.md ./publish/lightweight/
Compress-Archive -Path ./publish/lightweight/* -DestinationPath ./publish/speech2text-lightweight.zip

# 完全版のZIP作成（バージョン付き）
Copy-Item appsettings.template.json ./publish/standalone/appsettings.json -Force
Copy-Item README.md ./publish/standalone/
Compress-Archive -Path ./publish/standalone/* -DestinationPath ./publish/speech2text-v1.0.0-win-x64-standalone.zip -Force
```

**Linux/macOS環境での場合:**
```bash
# ZIPファイル作成（Linux/macOS）
mkdir -p ./publish
dotnet publish speech2text.csproj -c Release -r win-x64 --self-contained true -o ./publish/standalone
cp appsettings.template.json ./publish/standalone/appsettings.json
cp README.md ./publish/standalone/
cd ./publish && zip -r speech2text-v1.0.0-win-x64-standalone.zip ./standalone/*
```

**`dotnet publish`の選択肢:**

1. **Self-contained（推奨）**: 
   - .NET Runtime が含まれ、どの環境でもすぐに実行可能
   - ファイルサイズ: 約100-150MB
   - 企業環境での配布に最適

2. **Framework-dependent（軽量版）**: 
   - .NET 8.0 Runtime が事前にインストールされている必要
   - ファイルサイズ: 約5-10MB
   - 開発者向けまたはランタイムが既にある環境向け

## 使用方法

#### 開発環境での実行
```powershell
# 例
dotnet run "C:\audio\customer_call.wav"
```

#### 本番環境での実行（推奨）
```powershell
# 例
./publish/speech2text.exe "C:\audio\customer_call.wav"
```

**重要**: 配布されたファイルには、テンプレートの`appsettings.json`が含まれています。使用前に実際のAzureの設定値を入力してください。

### サポートされる音声ファイル形式

- WAV
- MP3（一部制限あり）
- その他 Azure Speech Serviceがサポートする形式

### 出力ファイル

1. `<音声ファイル名>.txt` - 文字起こし結果（タイムスタンプ付き）
2. `<音声ファイル名>_ai.txt` - AI分析結果

### 文字起こし出力例

```
[00:05] こんにちは、お電話ありがとうございます。
[00:08] 話者1: 本日はどのようなご用件でしょうか。
[00:12] 話者2: システムにログインできない問題でお電話しました。
[00:16] 話者1: 承知いたしました。確認いたします。
```

**注意**: 話者認識は音声品質や形式によって利用できない場合があります。その場合はタイムスタンプのみの出力になります。

## AI分析の内容

AI分析では以下の項目を出力します：

- **要約**: 通話内容の簡潔な要約
- **お客様の感情**: 感情の状態と具体的な理由
- **次のアクション**: 対応担当者が取るべき具体的な行動
- **重要なポイント**: 特に注意すべき点やお客様の要望

### AIプロンプトのカスタマイズ

`appsettings.json`の`AI.Prompts`セクションでAI分析の動作をカスタマイズできます：

- **SystemPrompt**: AIの役割と出力フォーマットを定義
- **UserPromptTemplate**: 分析対象のテキストをAIに渡す際のテンプレート（`{0}`が文字起こし結果に置き換えられます）

例えば、より詳細な感情分析や、特定の業界向けの分析を行いたい場合は、これらのプロンプトを編集してください。

## トラブルシューティング

### 音声認識がうまくいかない場合

1. **音声ファイルの形式を確認**
   - サポート形式：WAV, MP3, その他Azure Speech Serviceがサポートする形式

2. **音質が十分であることを確認**
   - クリアな音声であること
   - ノイズが少ないこと

3. **長い音声ファイルの場合**
   - このツールは継続的音声認識を使用し、長時間の音声にも対応しています
   - 認識中は進行状況がコンソールに表示されます
   - 処理時間は音声の長さに比例します

4. **Azure Speech Serviceの利用状況を確認**
   - APIキーとリージョンの設定を確認
   - 利用制限に達していないか確認

5. **話者認識について**
   - 話者認識は音声品質や録音環境に依存します
   - ステレオ音声（左右で別々の話者）の場合により効果的です
   - 話者認識が利用できない場合は自動的に通常認識にフォールバックします

### AI分析がうまくいかない場合

1. Azure OpenAIの利用状況を確認
2. デプロイメント名が正しいことを確認
3. APIキーの有効期限を確認

## ライセンス

このプロジェクトはMITライセンスの下で公開されています。

## GitHub Release作成手順

開発者向けのリリース作成手順：

```powershell
# 1. バージョンを決定（例：v1.0.0）
$version = "v1.0.0"

# 2. publishフォルダを作成
New-Item -ItemType Directory -Force -Path ./publish

# 3. ランタイム含む版をビルド
dotnet publish speech2text.csproj -c Release -r win-x64 --self-contained true -o ./publish/standalone
# 実際のappsettings.jsonではなく、テンプレートをappsettings.jsonとしてコピー
Copy-Item appsettings.template.json ./publish/standalone/appsettings.json -Force
Copy-Item README.md ./publish/standalone/

# 4. 軽量版をビルド
dotnet publish speech2text.csproj -c Release -r win-x64 --self-contained false -o ./publish/lightweight
# 実際のappsettings.jsonではなく、テンプレートをappsettings.jsonとしてコピー
Copy-Item appsettings.template.json ./publish/lightweight/appsettings.json -Force
Copy-Item README.md ./publish/lightweight/

# 5. ZIPファイルを作成
Compress-Archive -Path ./publish/standalone/* -DestinationPath "./publish/speech2text-$version-win-x64-standalone.zip" -Force
Compress-Archive -Path ./publish/lightweight/* -DestinationPath "./publish/speech2text-$version-win-x64-lightweight.zip" -Force

# 6. GitHubでタグを作成してReleaseページでZIPファイルをアップロード
```

### リリースファイル構成
作成されるファイル（`./publish/`フォルダ内）:
- `speech2text-v1.0.0-win-x64-standalone.zip` - ランタイム含む（推奨）
- `speech2text-v1.0.0-win-x64-lightweight.zip` - 軽量版（.NET 8.0必要）
