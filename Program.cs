using Microsoft.CognitiveServices.Speech;
using Microsoft.CognitiveServices.Speech.Audio;
using Microsoft.Extensions.Configuration;
using System.Text;
using System.Text.Json;

class Program
{
    private static IConfiguration? _configuration;
    
    static async Task Main(string[] args)
    {
        if (args.Length == 0)
        {
            Console.WriteLine("使用方法: speech2text.exe <音声ファイルパス>");
            Console.WriteLine("例: speech2text.exe C:\\path\\to\\audio.wav");
            return;
        }

        string audioFilePath = args[0];
        
        if (!File.Exists(audioFilePath))
        {
            Console.WriteLine($"エラー: ファイルが見つかりません: {audioFilePath}");
            return;
        }

        // 設定ファイルを読み込み
        _configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: false)
            .Build();

        try
        {
            Console.WriteLine("音声ファイルの文字起こしを開始しています...");
            
            // 音声を文字起こし
            string transcriptionText = await TranscribeAudioAsync(audioFilePath);
            
            // 文字起こし結果をファイルに保存
            string textFilePath = Path.ChangeExtension(audioFilePath, ".txt");
            await File.WriteAllTextAsync(textFilePath, transcriptionText, Encoding.UTF8);
            Console.WriteLine($"文字起こし完了: {textFilePath}");
            
            Console.WriteLine("AI分析を開始しています...");
            
            // AI分析を実行
            string analysisResult = await AnalyzeWithOpenAIAsync(transcriptionText);
            
            // AI分析結果をファイルに保存
            string baseFileName = Path.GetFileNameWithoutExtension(audioFilePath);
            string directory = Path.GetDirectoryName(audioFilePath) ?? "";
            string aiFilePath = Path.Combine(directory, $"{baseFileName}_ai.txt");
            await File.WriteAllTextAsync(aiFilePath, analysisResult, Encoding.UTF8);
            Console.WriteLine($"AI分析完了: {aiFilePath}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"エラーが発生しました: {ex.Message}");
        }
    }

    static async Task<string> TranscribeAudioAsync(string audioFilePath)
    {
        var speechKey = _configuration?["Azure:SpeechService:SubscriptionKey"];
        var speechRegion = _configuration?["Azure:SpeechService:Region"];

        if (string.IsNullOrEmpty(speechKey) || string.IsNullOrEmpty(speechRegion))
        {
            throw new InvalidOperationException("Azure Speech Serviceの設定が不正です。appsettings.jsonを確認してください。");
        }

        var speechConfig = SpeechConfig.FromSubscription(speechKey, speechRegion);
        speechConfig.SpeechRecognitionLanguage = "ja-JP";

        using var audioConfig = AudioConfig.FromWavFileInput(audioFilePath);
        using var speechRecognizer = new SpeechRecognizer(speechConfig, audioConfig);

        var result = await speechRecognizer.RecognizeOnceAsync();

        return result.Reason switch
        {
            ResultReason.RecognizedSpeech => result.Text,
            ResultReason.NoMatch => "音声を認識できませんでした。",
            ResultReason.Canceled => throw new Exception($"音声認識がキャンセルされました: {CancellationDetails.FromResult(result).ErrorDetails}"),
            _ => throw new Exception("音声認識に失敗しました。")
        };
    }    static async Task<string> AnalyzeWithOpenAIAsync(string transcriptionText)
    {
        var endpoint = _configuration?["Azure:OpenAI:Endpoint"];
        var apiKey = _configuration?["Azure:OpenAI:ApiKey"];
        var deploymentName = _configuration?["Azure:OpenAI:DeploymentName"];
        var systemPrompt = _configuration?["AI:Prompts:SystemPrompt"];
        var userPromptTemplate = _configuration?["AI:Prompts:UserPromptTemplate"];

        if (string.IsNullOrEmpty(endpoint) || string.IsNullOrEmpty(apiKey) || string.IsNullOrEmpty(deploymentName))
        {
            throw new InvalidOperationException("Azure OpenAIの設定が不正です。appsettings.jsonを確認してください。");
        }

        if (string.IsNullOrEmpty(systemPrompt) || string.IsNullOrEmpty(userPromptTemplate))
        {
            throw new InvalidOperationException("AIプロンプトの設定が不正です。appsettings.jsonを確認してください。");
        }

        // エンドポイントからベースURLを抽出
        var baseEndpoint = endpoint.Contains("/openai/deployments") 
            ? endpoint.Substring(0, endpoint.IndexOf("/openai/deployments"))
            : endpoint.TrimEnd('/');
        
        var requestUrl = $"{baseEndpoint}/openai/deployments/{deploymentName}/chat/completions?api-version=2024-02-15-preview";

        var requestBody = new
        {
            messages = new[]
            {
                new { role = "system", content = systemPrompt },
                new { role = "user", content = string.Format(userPromptTemplate, transcriptionText) }
            },
            max_tokens = 1000,
            temperature = 0.3
        };

        try
        {
            using var httpClient = new HttpClient();
            httpClient.DefaultRequestHeaders.Add("api-key", apiKey);
            
            var jsonContent = JsonSerializer.Serialize(requestBody);
            var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");
            
            var response = await httpClient.PostAsync(requestUrl, content);
            var responseContent = await response.Content.ReadAsStringAsync();
            
            if (!response.IsSuccessStatusCode)
            {
                return $"AI分析中にエラーが発生しました: {response.StatusCode} - {responseContent}";
            }

            var jsonResponse = JsonSerializer.Deserialize<JsonElement>(responseContent);
            var analysisResult = jsonResponse
                .GetProperty("choices")[0]
                .GetProperty("message")
                .GetProperty("content")
                .GetString();

            return analysisResult ?? "分析結果を取得できませんでした。";
        }
        catch (Exception ex)
        {
            return $"AI分析中にエラーが発生しました: {ex.Message}";
        }
    }
}
