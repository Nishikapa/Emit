namespace Hoge;

/// <summary>
/// Emit プロジェクトがラップ対象とするサンプルクラス。
/// 各静的メソッドが、生成される動的アセンブリのラップ対象になります。
/// </summary>
public static class HogeClass
{
    public static int Abc(string str)
    {
        Console.WriteLine("HogeClass::Abc");
        return str.Length;
    }

    public static int Bcd(string str1, string str2)
    {
        Console.WriteLine("HogeClass::Bcd");
        return str1.Length + str2.Length;
    }
}
