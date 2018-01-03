using System;

namespace 矩阵乘法
{
    internal class Program
    {
        private static void Main()
        {
            var a = new float[100, 100];
            var b = new float[100, 100];
            var c = new float[100, 100];

            var s = new float[100, 100];

            // 要求A*B矩阵的值，需保证A的列数=B的行数
            // 定义矩阵A的行列
            const int colA = 3;
            const int rowA = 2;
            // 定义矩阵B的行列
            const int colB = 2;
            const int rowB = 3;

            var random = new Random();

            Console.WriteLine("矩阵A：");
            for (var row = 0; row < rowA; row++)
            {
                for (var col = 0; col < colA; col++)
                {
                    a[row, col] = random.Next(1, 10);
                    Console.Write(" {0} ", a[row, col]);
                }
                Console.Write(Environment.NewLine);
            }
            Console.WriteLine("矩阵B：");
            for (var row = 0; row < rowB; row++)
            {
                for (var col = 0; col < colB; col++)
                {
                    b[row, col] = random.Next(1, 10);
                    Console.Write(" {0} ", b[row, col]);
                }
                Console.Write(Environment.NewLine);
            }
            Console.WriteLine("矩阵C=A*B");
            for (var row = 0; row < rowA; row++)
            {
                for (var col = 0; col < colB; col++)
                {
                    for (var i = 0; i < colA; i++)
                        s[row, col] += a[row, i] * b[i, col];
                    c[row, col] = s[row, col];
                    Console.Write(" {0} ", c[row, col]);
                }
                Console.Write(Environment.NewLine);
            }

            Console.ReadKey();
        }
    }
}
