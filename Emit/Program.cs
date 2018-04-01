using System;
using System.Reflection;
using System.Reflection.Emit;
using System.Linq;
using System.Collections.Generic;

namespace Emit
{
    static class Program
    {
        static void ForEach<T>(this IEnumerable<T> source, Action<T> func)
        {
            foreach (var s in source)
                func(s);
        }

        static void Main(string[] args)
        {
            var methodInfoWriteLine =
                typeof(Console).GetMethod("WriteLine", new[] { typeof(string) });

            Func<TypeBuilder, Action<MethodInfo>> MakeMethod = typeBuilder => methodInfo =>
            {
                var parameterInfos =
                    methodInfo
                    .GetParameters();

                var parameterTypes =
                    parameterInfos
                    .Select(pi => pi.ParameterType)
                    .ToArray();

                var methodBuilder =
                    typeBuilder.DefineMethod(
                        methodInfo.Name,
                        MethodAttributes.Public | MethodAttributes.Static,
                        methodInfo.ReturnType,
                        parameterTypes
                    );

                var generator = methodBuilder.GetILGenerator();

                generator.Emit(opcode: OpCodes.Ldstr, str: methodInfo.Name + " - Start");
                generator.Emit(opcode: OpCodes.Call, meth: methodInfoWriteLine);

                parameterInfos
                .Select((pi, i) => (pi, i))
                .ForEach(
                    t =>
                    {
                        var (pi, i) = t;
                        generator.Emit(opcode: OpCodes.Ldarg, arg: i);
                    }
                );

                generator.Emit(opcode: OpCodes.Call, meth: methodInfo);

                generator.Emit(opcode: OpCodes.Ldstr, methodInfo.Name + " - End");
                generator.Emit(opcode: OpCodes.Call, meth: methodInfoWriteLine);

                generator.Emit(opcode: OpCodes.Ret);
            };

            var assemblyBuilder = AssemblyBuilder.DefineDynamicAssembly(
                new AssemblyName("Test"),
                AssemblyBuilderAccess.Save
            );

            var moduleBuilder =
                assemblyBuilder.DefineDynamicModule("TestModule", "Test.dll");

            void MakeClass(Type typeClass)
            {
                var typeBuilder =
                    moduleBuilder.DefineType(typeClass.Name, TypeAttributes.Public | TypeAttributes.Class);

                var makeMethod = MakeMethod(typeBuilder);

                typeClass.GetMethods(BindingFlags.Static | BindingFlags.Public).ForEach(makeMethod);

                var ty = typeBuilder.CreateType();
            }

            MakeClass(typeof(Hoge.HogeClass));

            assemblyBuilder.Save("Test.dll");
        }
    }
}
