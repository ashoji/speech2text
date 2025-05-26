# Speech to Text with AI Analysis

Azure Speech to TextとAzure OpenAIを使用した音声文字起こし・AI分析ツール

## 概要

このツールは以下の機能を提供します：
1. Azure AI Service Speech to Textを使用した音声ファイルの文字起こし
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
  }
}
```

**注意**: 
- Endpointは、ベースURLのみを指定してください（`/openai/deployments/...`は含めない）
- DeploymentNameは、Azure OpenAIでデプロイしたモデルの名前を指定してください
- `appsettings.json`は機密情報を含むため、Gitにコミットされません

### 3. ビルドと実行

```powershell
# プロジェクトをビルド
dotnet build

# 実行
dotnet run "<音声ファイルパス>"
```

## 使用方法

```powershell
# 例
dotnet run "C:\audio\customer_call.wav"
```

### サポートされる音声ファイル形式

- WAV
- MP3（一部制限あり）
- その他Azure Speech Serviceがサポートする形式

### 出力ファイル

1. `<音声ファイル名>.txt` - 文字起こし結果
2. `<音声ファイル名>_ai.txt` - AI分析結果

## AI分析の内容

AI分析では以下の項目を出力します：

- **要約**: 通話内容の簡潔な要約
- **お客様の感情**: 感情の状態と具体的な理由
- **次のアクション**: 対応担当者が取るべき具体的な行動
- **重要なポイント**: 特に注意すべき点やお客様の要望

## トラブルシューティング

### 音声認識がうまくいかない場合

1. 音声ファイルの形式を確認
2. 音質が十分であることを確認
3. Azure Speech Serviceの利用状況を確認

### AI分析がうまくいかない場合

1. Azure OpenAIの利用状況を確認
2. デプロイメント名が正しいことを確認
3. APIキーの有効期限を確認

## ライセンス

このプロジェクトはMITライセンスの下で公開されています。
