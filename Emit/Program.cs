using System.Reflection;
using System.Reflection.Emit;
using Hoge;

// ラップ対象のクラス。この静的メソッド群を「Start / End ログ付き」で包んだ
// 同名メソッドを持つ型を、実行時に動的生成します。
Type target = typeof(HogeClass);

// .NET 9 以降の PersistedAssemblyBuilder はアセンブリをディスクへ保存できます
// （.NET Framework 時代の AssemblyBuilderAccess.Save 相当の後継 API）。
PersistedAssemblyBuilder assemblyBuilder = new(new AssemblyName("Test"), typeof(object).Assembly);
ModuleBuilder moduleBuilder = assemblyBuilder.DefineDynamicModule("TestModule");

TypeBuilder typeBuilder =
    moduleBuilder.DefineType(target.Name, TypeAttributes.Public | TypeAttributes.Class);

foreach (MethodInfo method in target.GetMethods(BindingFlags.Static | BindingFlags.Public))
    EmitLoggingWrapper(typeBuilder, method);

typeBuilder.CreateType();

// メタデータを生成して Test.dll として保存しつつ、同じイメージをその場で実行する。
using MemoryStream stream = new();
assemblyBuilder.Save(stream);

byte[] image = stream.ToArray();
File.WriteAllBytes("Test.dll", image);
Console.WriteLine($"Test.dll を生成しました（{image.Length:N0} bytes）。");
Console.WriteLine();

// 生成したアセンブリをロードし、ラップメソッドを呼び出して動作を確認する。
Type generated = Assembly.Load(image).GetType(target.Name)
    ?? throw new InvalidOperationException($"型 '{target.Name}' が見つかりません。");

Invoke(nameof(HogeClass.Abc), "Hello");
Console.WriteLine();
Invoke(nameof(HogeClass.Bcd), "Test", "Test");

void Invoke(string name, params object?[] args)
{
    object? result = generated.GetMethod(name)!.Invoke(null, args);
    Console.WriteLine($"=> {result}");
}

// 1 つの静的メソッドを、前後にログ出力を挟むラッパーとして再定義する。
static void EmitLoggingWrapper(TypeBuilder typeBuilder, MethodInfo method)
{
    ParameterInfo[] parameters = method.GetParameters();

    MethodBuilder wrapper = typeBuilder.DefineMethod(
        method.Name,
        MethodAttributes.Public | MethodAttributes.Static,
        method.ReturnType,
        Array.ConvertAll(parameters, p => p.ParameterType));

    MethodInfo writeLine = typeof(Console).GetMethod(nameof(Console.WriteLine), [typeof(string)])!;
    ILGenerator il = wrapper.GetILGenerator();

    // Console.WriteLine($"{name} - Start");
    il.Emit(OpCodes.Ldstr, $"{method.Name} - Start");
    il.Emit(OpCodes.Call, writeLine);

    // 引数をすべて積んで元メソッドを呼ぶ。戻り値は評価スタックに残したまま、
    // 終了ログを出力してから Ret することで、元の戻り値をそのまま返す。
    for (int i = 0; i < parameters.Length; i++)
        EmitLoadArgument(il, i);
    il.Emit(OpCodes.Call, method);

    // Console.WriteLine($"{name} - End");
    il.Emit(OpCodes.Ldstr, $"{method.Name} - End");
    il.Emit(OpCodes.Call, writeLine);

    il.Emit(OpCodes.Ret);
}

// 引数 index を読み込む最適な IL 命令を発行する（静的メソッドなので index は第 1 引数が 0）。
static void EmitLoadArgument(ILGenerator il, int index)
{
    switch (index)
    {
        case 0: il.Emit(OpCodes.Ldarg_0); break;
        case 1: il.Emit(OpCodes.Ldarg_1); break;
        case 2: il.Emit(OpCodes.Ldarg_2); break;
        case 3: il.Emit(OpCodes.Ldarg_3); break;
        default: il.Emit(OpCodes.Ldarg_S, (byte)index); break;
    }
}
