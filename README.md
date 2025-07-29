# 📊 Unity UI Toolkit チャートコンポーネント

[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](https://opensource.org/licenses/MIT)
[![Unity Version](https://img.shields.io/badge/Unity-2021.3%2B-blue.svg)](https://unity.com/)

Unity UI Toolkit向けのカスタマイズ可能なチャートコンポーネントです。複数のデータセット、グリッド表示、インタラクティブなツールチップをサポートしています。

## 主な機能

- 🚀 UI Toolkitと簡単に統合可能
- 📊 複数のデータセットをサポート
- 🎨 見た目のカスタマイズ（色、線の太さ、グリッドなど）
- 🔍 ホバー時のインタラクティブなツールチップ
- 📱 レスポンシブデザイン

## インストール方法

1. [リリースページ](https://github.com/Rimuru0525/unity-ui-chart/releases)から最新の`.unitypackage`をダウンロード
2. Unityプロジェクトにインポート（Assets > Import Package > Custom Package...）
3. UI ToolkitのUXMLファイルで`ChartElement`を使用開始

## クイックスタート

1. 新しいUI Documentを作成するか、既存のものを使用
2. UXMLファイルに以下を追加:

```xml
<ui:UXML xmlns:ui="UnityEngine.UIElements">
    <ChartElement name="chart" class="chart-element" />
</ui:UXML>
```

3. UIDocumentと同じGameObjectに`ChartController`コンポーネントを追加
4. インスペクターでチャートの設定を構成

## 使用例

```csharp
// Get a reference to your chart element
var chart = rootVisualElement.Q<ChartElement>("chart");

// Create and add a dataset
var dataset = new ChartData 
{
    name = "Sample Data",
    color = Color.blue,
    points = new List<DataPoint>()
};

// Add some data points
for (float x = 0; x < 10; x += 0.5f)
{
    dataset.points.Add(new DataPoint(x, Mathf.Sin(x)));
}

// Add the dataset to the chart
chart.AddDataset(dataset);

// Refresh the chart
chart.Refresh();
```

## ライセンス

このプロジェクトは [MIT ライセンス](LICENSE)の下で公開されています。

## コントリビューション

バグの報告や機能のリクエストは [Issue](https://github.com/Rimuru0525/unity-ui-chart/issues) からお願いします。

プルリクエストも歓迎しています。以下の手順でお願いします：

1. リポジトリをフォークする
2. 機能ブランチを作成する (`git checkout -b feature/AmazingFeature`)
3. 変更をコミットする (`git commit -m 'Add some AmazingFeature'`)
4. ブランチにプッシュする (`git push origin feature/AmazingFeature`)
5. プルリクエストを開く

## 作者

- [@Rimuru0525](https://github.com/Rimuru0525)

## 謝辞

- このプロジェクトに貢献してくださった全てのコントリビューターの皆様に感謝します。

## License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.
## Contributing

Contributions are welcome! Please feel free to submit a Pull Request.
