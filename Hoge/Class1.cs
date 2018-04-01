using System;

namespace Hoge
{
    public class HogeClass
    {
        static public int Abc(string str)
        {
            Console.WriteLine("HogeClass::Abc");
            return str.Length;
        }
        static public int Bcd(string str1, string str2)
        {
            Console.WriteLine("HogeClass::Bcd");
            return str1.Length+str2.Length;
        }
    }
}
