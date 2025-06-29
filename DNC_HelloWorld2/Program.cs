using System;

using C_Interpreter_Impl;

internal class Program
{
    static void Main(string[] args)
    {
        Console.WriteLine("Hello, World!");

        string cTestProgramSrc = """
            
            int count = 0;

            int tarai(int x, int y, int z) {
            	count++;
            	if (x <= y) {
            		return y;
            	}
            	return tarai(
            		tarai(x - 1, y, z),
            		tarai(y - 1, z, x),
            		tarai(z - 1, x, y));
            }

            void multiple_by(void *ptr, int x)
            {
            	signed int *a = (signed int *)ptr;

            	*a = *a * x;
            }

            int test(int a)
            {
            	return a * a;
            }

            int main()
            {
            	char tmp[128] = { 0 };

            	int ans;

            	int a = 1;

            	printf("Hello World!\n");

            	printf("--Test Begin--\n");

            	for (int i = 0;i < 4;i++)
            	{
            		sprintf(tmp, "%d * %d = %d", a, a, test(a));
            		printf("%s\n", tmp);

            		multiple_by(&a, 2);
            	}

            	ans = tarai(3, 2, 0);
            	printf("ans = %d\n", ans);
            	printf("count = %d\n", count);
            	printf("--Test End--\n");

            	return ans;
            }

            
""";

        int ret = C_Interpreter_Test_Class.RunC(cTestProgramSrc);
        
        Console.WriteLine("Return code = " + ret);
    }
}

public static class C_Interpreter_Test_Class
{
    public static int RunC(string cProgramBody)
    {
        // ここに RunC の実体を実装せよ。

        // TODO
        return 123;
    }
}


namespace C_Interpreter_Impl
{
    // ここにクラスを色々作成し、C インタプリタを実装せよ。

    // TODO
}

