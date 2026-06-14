# Emit

`System.Reflection.Emit` を使って、**既存メソッドを「開始 / 終了ログ付き」で包む型を実行時に動的生成する**デモです。
AOP（アスペクト指向）のメソッドインターセプトを、IL を直接組み立てて実現する最小例になっています。

## 何をするか

[`Hoge.HogeClass`](Hoge/HogeClass.cs) の各静的メソッドに対して、同じシグネチャを持つラッパーメソッドを動的に生成します。
生成されたラッパーは次の IL を実行します。

```text
Console.WriteLine("<メソッド名> - Start");
var result = 元のメソッド(引数...);   // 戻り値は評価スタックに残す
Console.WriteLine("<メソッド名> - End");
return result;                          // 元の戻り値をそのまま返す
```

生成したアセンブリは `Test.dll` として保存し、さらにその場でロードして呼び出すことで動作を確認します。

### 実行結果

```text
Test.dll を生成しました（2,560 bytes）。

Abc - Start
HogeClass::Abc
Abc - End
=> 5

Bcd - Start
HogeClass::Bcd
Bcd - End
=> 8
```

## 構成

| プロジェクト | 種別 | 役割 |
| --- | --- | --- |
| [`Hoge`](Hoge/HogeClass.cs) | ライブラリ | ラップ対象となるサンプルメソッド（`Abc` / `Bcd`）を提供 |
| [`Emit`](Emit/Program.cs) | コンソールアプリ | 動的アセンブリを生成・保存し、その場でロードして実行 |

## 動かし方

[.NET 10 SDK](https://dotnet.microsoft.com/download) が必要です。

```bash
dotnet run --project Emit
```

`Test.dll` はカレントディレクトリに出力されます（`.gitignore` 済み）。

ビルドのみ行う場合:

```bash
dotnet build
```

## 技術メモ

- **`PersistedAssemblyBuilder`** … 動的アセンブリのディスク保存（旧 `AssemblyBuilderAccess.Save` 相当）は
  .NET Core / .NET 5〜8 では利用できませんでしたが、**.NET 9 で `PersistedAssemblyBuilder` として復活**しました。
  本デモはこれを利用しています。
- **戻り値の引き回し** … 元メソッドの呼び出し後、戻り値を評価スタックに残したまま終了ログを出力し、
  最後に `Ret` することで、ラッパーが元の戻り値をそのまま返します。
- **引数ロードの最適化** … 引数番号に応じて `Ldarg_0`〜`Ldarg_3` / `Ldarg_S` を使い分けています。

## 環境

- .NET 10 / C# 14
- ソリューション形式: `.slnx`（XML ベースの新形式）
