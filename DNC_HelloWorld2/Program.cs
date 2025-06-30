using System;
using System.Collections.Generic;
using System.Linq;
// -- AI Hint Begin --
// [A] 全体の実装方針メモ
// C89 世代の C 言語を解釈するインタプリタを C# で実装する。
// 4KB 単位で管理する仮想メモリを持ち、AST を解釈することで
// main 関数を実行する仕組みを作る。
// Step 1: 字句解析と構文解析の骨組みを作成
// Step 2: 仮想メモリ・型管理の基礎を実装
// Step 3: インタプリタの実行エンジンを作成
// Step 4: 標準ライブラリ相当の組み込み関数実装
// Step 5: テストプログラムを実行し仕様を満たす
//
// [B] 現在の実装状況メモ
// 字句解析器(Lexer)はおおむね完成。Parser は return 文と
// 式文のみ解析できるようになり、Interpreter もそれらの
// 実行に対応した。ただし if 文や for 文、変数宣言、
// ポインタ・配列などは未対応。
//
// [C] 現在着手している項目における未完成項目
// - if, for など複雑なステートメントの解析
// - 変数宣言・ローカル変数管理
// - 組み込み関数や仮想メモリの詳細実装
//
// [D] 次の AI 呼出しの際に、次の AI が参考にすべき、今の AI が書き残したい事柄
// 次回は ++ 演算子や for 文など、テストプログラムで使われている
// 機能を解析・実行できるように拡張すること。特にループと
// 変数宣言の扱いを追加すべき。
// -- AI Hint End --

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
                char tmp2[128] = { 0 };

            	int ans;

            	int a = 1;

            	printf("Hello World!\n");

            	printf("--Test Begin--\n");

            	for (int i = 0;i < 4;i++)
            	{
            		sprintf(tmp2, "%d * %d = %d", a, a, test(a));
            		printf("%s\n", tmp);

            		multiple_by(&a, 2);
            	}

                int x = test(3);
                x++;
                printf("%d\n",x);

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
        // ここで C インタプリタを呼び出す。
        var interpreter = new Interpreter();

        // 実行結果を返す。現在は未実装のため例外を送出する。
        return interpreter.Execute(cProgramBody);
    }
}


namespace C_Interpreter_Impl
{
    // ここにクラスを色々作成し、C インタプリタを実装せよ。

    /// <summary>
    /// 字句解析器のトークン種別
    /// </summary>
    internal enum TokenKind
    {
        Identifier,
        Number,
        String,
        Keyword,
        Symbol,
        End,
    }

    /// <summary>
    /// トークンを表すクラス
    /// </summary>
    internal record Token(string Text, TokenKind Kind);

    /// <summary>
    /// 字句解析を行うクラス (未実装)
    /// </summary>
    internal class Lexer
    {
        private readonly string _source;

        public Lexer(string source)
        {
            _source = source;
        }

        public IEnumerable<Token> Tokenize()
        {
            var tokens = new List<Token>();
            int i = 0;

            while (i < _source.Length)
            {
                char c = _source[i];

                // 空白や改行をスキップ
                if (char.IsWhiteSpace(c))
                {
                    i++;
                    continue;
                }

                // 文字列リテラル
                if (c == '"')
                {
                    int start = ++i;
                    while (i < _source.Length && _source[i] != '"') i++;
                    string str = _source.Substring(start, i - start);
                    tokens.Add(new Token(str, TokenKind.String));
                    if (i < _source.Length) i++; // 終端の"を飛ばす
                    continue;
                }

                // 数値リテラル
                if (char.IsDigit(c))
                {
                    int start = i;
                    while (i < _source.Length && char.IsDigit(_source[i])) i++;
                    tokens.Add(new Token(_source.Substring(start, i - start), TokenKind.Number));
                    continue;
                }

                // 識別子 または キーワード
                if (char.IsLetter(c) || c == '_')
                {
                    int start = i;
                    while (i < _source.Length && (char.IsLetterOrDigit(_source[i]) || _source[i] == '_')) i++;
                    string text = _source.Substring(start, i - start);
                    if (IsKeyword(text))
                        tokens.Add(new Token(text, TokenKind.Keyword));
                    else
                        tokens.Add(new Token(text, TokenKind.Identifier));
                    continue;
                }

                // 複合記号 (<=, >=, ==, !=, ++, --)
                if (i + 1 < _source.Length)
                {
                    string two = _source.Substring(i, 2);
                    if (two == "<=" || two == ">=" || two == "==" || two == "!=" || two == "++" || two == "--" || two == "&&" || two == "||")
                    {
                        tokens.Add(new Token(two, TokenKind.Symbol));
                        i += 2;
                        continue;
                    }
                }

                // 単一記号
                tokens.Add(new Token(c.ToString(), TokenKind.Symbol));
                i++;
            }

            tokens.Add(new Token(string.Empty, TokenKind.End));
            return tokens;
        }

        private static readonly HashSet<string> _keywords = new() { "int", "void", "char", "return", "if", "else", "for", "signed", "unsigned" };

        private static bool IsKeyword(string text) => _keywords.Contains(text);
    }

    /// <summary>
    /// 抽象構文木の基底クラス (詳細は未実装)
    /// </summary>
    internal abstract class AstNode { }

    /// <summary>
    /// プログラム全体を表す AST
    /// </summary>
    internal class ProgramAst : AstNode
    {
        public List<FunctionDef> Functions { get; } = new();
        public List<VariableDecl> Globals { get; } = new();
    }

    // 変数を参照するポインタ表現
    internal class PointerValue
    {
        public Dictionary<string, object> Scope { get; }
        public string Name { get; }
        public PointerValue(Dictionary<string, object> scope, string name)
        {
            Scope = scope;
            Name = name;
        }
        public object Get() => Scope.TryGetValue(Name, out var v) ? v : 0;
        public void Set(object v) => Scope[Name] = v;
    }

    /// <summary>
    /// 変数宣言
    /// </summary>
    internal class VariableDecl : AstNode
    {
        public string Type { get; }
        public string Name { get; }

        public VariableDecl(string type, string name)
        {
            Type = type;
            Name = name;
        }
    }

    /// <summary>
    /// 関数定義
    /// </summary>
    internal class FunctionDef : AstNode
    {
        public string ReturnType { get; }
        public string Name { get; }
        public List<(string Type, string Name)> Parameters { get; }
        public List<AstNode> Body { get; }

        public FunctionDef(string returnType, string name, List<(string, string)> parameters, List<AstNode> body)
        {
            ReturnType = returnType;
            Name = name;
            Parameters = parameters;
            Body = body;
        }
    }

    // --- 以下、式および文を表す AST クラス群 ---

    internal abstract class Expr : AstNode { }

    internal class NumberExpr : Expr
    {
        public int Value { get; }
        public NumberExpr(int value) { Value = value; }
    }

    internal class StringExpr : Expr
    {
        public string Value { get; }
        public StringExpr(string value) { Value = value; }
    }

    internal class IdentifierExpr : Expr
    {
        public string Name { get; }
        public IdentifierExpr(string name) { Name = name; }
    }

    internal class UnaryExpr : Expr
    {
        public string Op { get; }
        public Expr Operand { get; }
        public UnaryExpr(string op, Expr operand)
        {
            Op = op;
            Operand = operand;
        }
    }

    internal class BinaryExpr : Expr
    {
        public string Op { get; }
        public Expr Left { get; }
        public Expr Right { get; }
        public BinaryExpr(string op, Expr left, Expr right)
        {
            Op = op;
            Left = left;
            Right = right;
        }
    }

    internal class CallExpr : Expr
    {
        public string FuncName { get; }
        public List<Expr> Args { get; }
        public CallExpr(string funcName, List<Expr> args)
        {
            FuncName = funcName;
            Args = args;
        }
    }

    internal abstract class Stmt : AstNode { }

    internal class ReturnStmt : Stmt
    {
        public Expr? Expr { get; }
        public ReturnStmt(Expr? expr) { Expr = expr; }
    }

    internal class ExprStmt : Stmt
    {
        public Expr Expression { get; }
        public ExprStmt(Expr expr) { Expression = expr; }
    }

    // 新しい文型: 変数宣言
    internal class VarDeclStmt : Stmt
    {
        public string Type { get; }
        public string Name { get; }
        public Expr? Init { get; }
        public VarDeclStmt(string type, string name, Expr? init)
        {
            Type = type;
            Name = name;
            Init = init;
        }
    }

    // if 文
    internal class IfStmt : Stmt
    {
        public Expr Condition { get; }
        public List<Stmt> ThenStmts { get; }
        public List<Stmt>? ElseStmts { get; }
        public IfStmt(Expr cond, List<Stmt> thenStmts, List<Stmt>? elseStmts)
        {
            Condition = cond;
            ThenStmts = thenStmts;
            ElseStmts = elseStmts;
        }
    }

    // for 文
    internal class ForStmt : Stmt
    {
        public Stmt Init { get; }
        public Expr Condition { get; }
        public Expr Post { get; }
        public List<Stmt> Body { get; }
        public ForStmt(Stmt init, Expr cond, Expr post, List<Stmt> body)
        {
            Init = init;
            Condition = cond;
            Post = post;
            Body = body;
        }
    }

    // ブロック文
    internal class BlockStmt : Stmt
    {
        public List<Stmt> Statements { get; }
        public BlockStmt(List<Stmt> stmts) { Statements = stmts; }
    }

    // 単に何もしない文
    internal class EmptyStmt : Stmt { }

    /// <summary>
    /// 構文解析クラス (未実装)
    /// </summary>
    internal class Parser
    {
        private readonly List<Token> _tokens;
        private int _index;

        public Parser(IEnumerable<Token> tokens)
        {
            _tokens = tokens.ToList();
            _index = 0;
        }

        private Token Current => _tokens[Math.Min(_index, _tokens.Count - 1)];

        private Token Consume()
        {
            var t = Current;
            if (_index < _tokens.Count) _index++;
            return t;
        }

        private Token Expect(string text)
        {
            if (Current.Text != text) throw new Exception($"expected '{text}' but got '{Current.Text}'");
            return Consume();
        }

        private string ExpectIdentifier()
        {
            if (Current.Kind != TokenKind.Identifier) throw new Exception("identifier expected");
            return Consume().Text;
        }

        private string ReadType()
        {
            var parts = new List<string>();
            while (Current.Kind == TokenKind.Keyword || Current.Text == "*")
            {
                parts.Add(Consume().Text);
            }
            if (parts.Count == 0) throw new Exception("type expected");
            return string.Join(" ", parts);
        }

        private bool Match(string text)
        {
            if (Current.Text == text)
            {
                Consume();
                return true;
            }
            return false;
        }

        // --- 式の解析 ---
        private static int GetPrecedence(string op) => op switch
        {
            "=" => 0,
            "||" => 1,
            "&&" => 2,
            "==" or "!=" => 3,
            "<" or ">" or "<=" or ">=" => 4,
            "+" or "-" => 5,
            "*" or "/" => 6,
            _ => -1,
        };

        private Expr ParseUnary()
        {
            if (Match("++")) return new UnaryExpr("pre++", ParseUnary());
            if (Match("--")) return new UnaryExpr("pre--", ParseUnary());
            if (Match("&")) return new UnaryExpr("&", ParseUnary());
            if (Match("*")) return new UnaryExpr("*", ParseUnary());
            if (Match("-")) return new UnaryExpr("neg", ParseUnary());
            return ParsePostfix();
        }

        private Expr ParsePostfix()
        {
            Expr expr = ParsePrimary();
            while (true)
            {
                if (Match("++"))
                {
                    expr = new UnaryExpr("post++", expr);
                    continue;
                }
                if (Match("--"))
                {
                    expr = new UnaryExpr("post--", expr);
                    continue;
                }
                break;
            }
            return expr;
        }

        private Expr ParsePrimary()
        {
            if (Current.Kind == TokenKind.Number)
            {
                int v = int.Parse(Consume().Text);
                return new NumberExpr(v);
            }
            if (Current.Kind == TokenKind.String)
            {
                string s = Consume().Text;
                return new StringExpr(s);
            }
            if (Current.Kind == TokenKind.Identifier)
            {
                string name = Consume().Text;
                if (Match("("))
                {
                    var args = new List<Expr>();
                    if (!Match(")"))
                    {
                        while (true)
                        {
                            args.Add(ParseExpression());
                            if (Match(","))
                                continue;
                            Expect(")");
                            break;
                        }
                    }
                    return new CallExpr(name, args);
                }
                return new IdentifierExpr(name);
            }
            if (Match("("))
            {
                if (IsTypeKeyword(Current) || Current.Text == "*")
                {
                    while (Current.Text != ")") Consume();
                    Expect(")");
                    // キャストは無視してその後の式を解析
                    return ParseUnary();
                }
                var e = ParseExpression();
                Expect(")");
                return e;
            }
            throw new Exception($"unexpected token {Current.Text}");
        }

        private Expr ParseExpression(int prec = 0)
        {
            Expr left = ParseUnary();
            while (true)
            {
                string op = Current.Text;
                int opPrec = GetPrecedence(op);
                if (opPrec < prec)
                    break;
                Consume();
                Expr right = ParseExpression(opPrec + 1);
                left = new BinaryExpr(op, left, right);
            }
            return left;
        }

        // --- 文の解析 ---
        private bool IsTypeKeyword(Token t) => t.Kind == TokenKind.Keyword;

        private List<Stmt> ParseBlock()
        {
            var list = new List<Stmt>();
            Expect("{");
            while (Current.Text != "}")
            {
                list.Add(ParseStatement());
            }
            Expect("}");
            return list;
        }

        private Stmt ParseStatement()
        {
            if (Match("{"))
            {
                // ブロック
                var list = new List<Stmt>();
                while (Current.Text != "}") list.Add(ParseStatement());
                Expect("}");
                return new BlockStmt(list);
            }

            if (Match("return"))
            {
                Expr? expr = null;
                if (Current.Text != ";")
                    expr = ParseExpression();
                Expect(";");
                return new ReturnStmt(expr);
            }

            if (Match("if"))
            {
                Expect("(");
                var cond = ParseExpression();
                Expect(")");
                var thenStmt = ParseStatement();
                List<Stmt> thenList = thenStmt is BlockStmt b ? b.Statements : new List<Stmt> { thenStmt };
                List<Stmt>? elseList = null;
                if (Match("else"))
                {
                    var es = ParseStatement();
                    elseList = es is BlockStmt eb ? eb.Statements : new List<Stmt> { es };
                }
                return new IfStmt(cond, thenList, elseList);
            }

            if (Match("for"))
            {
                Expect("(");
                Stmt init;
                if (Current.Text != ";")
                {
                    if (IsTypeKeyword(Current))
                    {
                        string t = ReadType();
                        string n = ExpectIdentifier();
                        Expr? initExpr = null;
                        if (Match("=")) initExpr = ParseExpression();
                        Expect(";");
                        init = new VarDeclStmt(t, n, initExpr);
                    }
                    else
                    {
                        var ie = ParseExpression();
                        Expect(";");
                        init = new ExprStmt(ie);
                    }
                }
                else
                {
                    Expect(";");
                    init = new EmptyStmt();
                }
                Expr cond = Current.Text != ";" ? ParseExpression() : new NumberExpr(1);
                Expect(";");
                Expr post = Current.Text != ")" ? ParseExpression() : new NumberExpr(0);
                Expect(")");
                var bodyStmt = ParseStatement();
                List<Stmt> body = bodyStmt is BlockStmt bb ? bb.Statements : new List<Stmt> { bodyStmt };
                return new ForStmt(init, cond, post, body);
            }

            if (IsTypeKeyword(Current))
            {
                string t = ReadType();
                string n = ExpectIdentifier();
                if (Match("["))
                {
                    // 配列サイズは現在未使用のため読み飛ばす
                    while (!Match("]")) Consume();
                }
                Expr? init = null;
                if (Match("="))
                {
                    if (Match("{"))
                    {
                        init = ParseExpression();
                        Expect("}");
                    }
                    else
                    {
                        init = ParseExpression();
                    }
                }
                Expect(";");
                return new VarDeclStmt(t, n, init);
            }

            var e = ParseExpression();
            Expect(";");
            return new ExprStmt(e);
        }

        public ProgramAst Parse()
        {
            var prog = new ProgramAst();

            while (Current.Kind != TokenKind.End)
            {
                string type = ReadType();
                string name = ExpectIdentifier();

                if (Current.Text == "(")
                {
                    Consume(); // (
                    var parameters = new List<(string Type, string Name)>();
                    if (Current.Text != ")")
                    {
                        while (true)
                        {
                            string pType = ReadType();
                            string pName = ExpectIdentifier();
                            parameters.Add((pType, pName));
                            if (Current.Text == ",")
                            {
                                Consume();
                                continue;
                            }
                            break;
                        }
                    }
                    Expect(")");

                    Expect("{");
                    int level = 1;
                    var bodyTokens = new List<Token>();
                    while (level > 0)
                    {
                        var t = Consume();
                        if (t.Text == "{") level++;
                        else if (t.Text == "}") level--;
                        if (level > 0) bodyTokens.Add(t);
                    }

                    bodyTokens.Add(new Token(string.Empty, TokenKind.End));
                    var bodyParser = new Parser(bodyTokens);
                    var bodyList = new List<AstNode>();
                    while (bodyParser.Current.Kind != TokenKind.End)
                    {
                        bodyList.Add(bodyParser.ParseStatement());
                    }

                    prog.Functions.Add(new FunctionDef(type, name, parameters, bodyList));
                }
                else
                {
                    // グローバル変数宣言 (初期値等は未処理)
                    if (Current.Text == "=")
                    {
                        // = xxx を読み飛ばす
                        Consume();
                        while (Current.Text != ";") Consume();
                    }
                    Expect(";");
                    prog.Globals.Add(new VariableDecl(type, name));
                }
            }

            return prog;
        }
    }

    /// <summary>
    /// C プログラムを実行するインタプリタ (未実装)
    /// </summary>
    internal class Interpreter
    {
        private readonly Dictionary<string, object> _globals = new();
        private readonly Dictionary<string, FunctionDef> _functions = new();

        /// <summary>
        /// プログラムを実行する
        /// </summary>
        public int Execute(string source)
        {
            // まず字句解析と構文解析を行う
            var lexer = new Lexer(source);
            var tokens = lexer.Tokenize();
            var parser = new Parser(tokens);
            ProgramAst ast = parser.Parse();

            foreach (var g in ast.Globals)
            {
                _globals[g.Name] = 0;
            }
            foreach (var f in ast.Functions)
            {
                _functions[f.Name] = f;
            }

            if (!_functions.TryGetValue("main", out var mainFunc))
                throw new Exception("main not found");

            var ret = EvaluateFunction(mainFunc, new List<object>());
            return ret is int i ? i : 0;
        }

        private object? EvaluateFunction(FunctionDef func, List<object> args)
        {
            var locals = new Dictionary<string, object>();
            for (int i = 0; i < func.Parameters.Count && i < args.Count; i++)
            {
                locals[func.Parameters[i].Name] = args[i];
            }

            foreach (var stmt in func.Body)
            {
                var result = ExecuteStatement(stmt, locals);
                if (result is not null)
                    return result;
            }
            return null;
        }

        private object? ExecuteStatement(AstNode stmt, Dictionary<string, object> locals)
        {
            switch (stmt)
            {
                case ReturnStmt r:
                    return r.Expr is null ? 0 : EvaluateExpr(r.Expr, locals);
                case ExprStmt e:
                    _ = EvaluateExpr(e.Expression, locals);
                    return null;
                case VarDeclStmt v:
                    locals[v.Name] = v.Init is null ? 0 : EvaluateExpr(v.Init, locals);
                    return null;
                case BlockStmt b:
                    foreach (var s in b.Statements)
                    {
                        var res = ExecuteStatement(s, locals);
                        if (res is not null) return res;
                    }
                    return null;
                case IfStmt ifs:
                    if (Convert.ToInt32(EvaluateExpr(ifs.Condition, locals)) != 0)
                    {
                        foreach (var s in ifs.ThenStmts)
                        {
                            var r = ExecuteStatement(s, locals);
                            if (r is not null) return r;
                        }
                    }
                    else if (ifs.ElseStmts != null)
                    {
                        foreach (var s in ifs.ElseStmts)
                        {
                            var r = ExecuteStatement(s, locals);
                            if (r is not null) return r;
                        }
                    }
                    return null;
                case ForStmt fs:
                    ExecuteStatement(fs.Init, locals);
                    while (Convert.ToInt32(EvaluateExpr(fs.Condition, locals)) != 0)
                    {
                        foreach (var s in fs.Body)
                        {
                            var r = ExecuteStatement(s, locals);
                            if (r is not null) return r;
                        }
                        _ = EvaluateExpr(fs.Post, locals);
                    }
                    return null;
                default:
                    throw new NotImplementedException();
            }
        }

        private object EvaluateExpr(Expr expr, Dictionary<string, object> locals)
        {
            switch (expr)
            {
                case NumberExpr n:
                    return n.Value;
                case StringExpr s:
                    return s.Value;
                case IdentifierExpr id:
                    if (locals.TryGetValue(id.Name, out var lv)) return lv;
                    if (_globals.TryGetValue(id.Name, out var gv)) return gv;
                    throw new Exception($"unknown identifier {id.Name}");
                case UnaryExpr u:
                    switch (u.Op)
                    {
                        case "&":
                            if (u.Operand is IdentifierExpr iid)
                            {
                                if (locals.ContainsKey(iid.Name))
                                    return new PointerValue(locals, iid.Name);
                                if (_globals.ContainsKey(iid.Name))
                                    return new PointerValue(_globals, iid.Name);
                            }
                            throw new Exception("invalid & operand");
                        case "*":
                            var pv = EvaluateExpr(u.Operand, locals) as PointerValue ?? throw new Exception("invalid pointer");
                            return pv.Get();
                        case "neg":
                            return -Convert.ToInt32(EvaluateExpr(u.Operand, locals));
                        case "pre++":
                            var lval1 = GetLValue(u.Operand, locals);
                            int v1 = Convert.ToInt32(lval1.Get()) + 1;
                            lval1.Set(v1);
                            return v1;
                        case "post++":
                            var lval2 = GetLValue(u.Operand, locals);
                            int old = Convert.ToInt32(lval2.Get());
                            lval2.Set(old + 1);
                            return old;
                        case "pre--":
                            var lval3 = GetLValue(u.Operand, locals);
                            int v2 = Convert.ToInt32(lval3.Get()) - 1;
                            lval3.Set(v2);
                            return v2;
                        case "post--":
                            var lval4 = GetLValue(u.Operand, locals);
                            int old2 = Convert.ToInt32(lval4.Get());
                            lval4.Set(old2 - 1);
                            return old2;
                        default:
                            throw new NotImplementedException(u.Op);
                    }
                case BinaryExpr bin:
                    if (bin.Op == "=")
                    {
                        var lval = GetLValue(bin.Left, locals);
                        var rv = EvaluateExpr(bin.Right, locals);
                        lval.Set(rv);
                        return rv;
                    }
                    int li = Convert.ToInt32(EvaluateExpr(bin.Left, locals));
                    int ri = Convert.ToInt32(EvaluateExpr(bin.Right, locals));
                    return bin.Op switch
                    {
                        "+" => li + ri,
                        "-" => li - ri,
                        "*" => li * ri,
                        "/" => li / ri,
                        "<" => li < ri ? 1 : 0,
                        "<=" => li <= ri ? 1 : 0,
                        ">" => li > ri ? 1 : 0,
                        ">=" => li >= ri ? 1 : 0,
                        "==" => li == ri ? 1 : 0,
                        "!=" => li != ri ? 1 : 0,
                        "&&" => (li != 0 && ri != 0) ? 1 : 0,
                        "||" => (li != 0 || ri != 0) ? 1 : 0,
                        _ => throw new NotImplementedException($"operator {bin.Op}"),
                    };
                case CallExpr call:
                    if (call.FuncName == "sprintf" && call.Args.Count > 0)
                    {
                        var dest = GetLValue(call.Args[0], locals);
                        var list = new List<object> { dest };
                        for (int i = 1; i < call.Args.Count; i++)
                            list.Add(EvaluateExpr(call.Args[i], locals));
                        return CallFunction(call.FuncName, list);
                    }
                    else
                    {
                        var argVals = call.Args.Select(a => EvaluateExpr(a, locals)).ToList();
                        return CallFunction(call.FuncName, argVals);
                    }
                default:
                    throw new NotImplementedException();
            }
        }

        private PointerValue GetLValue(Expr expr, Dictionary<string, object> locals)
        {
            if (expr is IdentifierExpr id)
            {
                if (locals.ContainsKey(id.Name)) return new PointerValue(locals, id.Name);
                if (_globals.ContainsKey(id.Name)) return new PointerValue(_globals, id.Name);
            }
            if (expr is UnaryExpr u && u.Op == "*")
            {
                var pv = EvaluateExpr(u.Operand, locals) as PointerValue ?? throw new Exception("invalid pointer");
                return pv;
            }
            throw new Exception("not an lvalue");
        }

        private object CallFunction(string name, List<object> args)
        {
            // 組み込み関数
            if (name == "printf")
            {
                if (args.Count > 0 && args[0] is string fmt)
                {
                    var converted = ConvertPrintfFormat(fmt, args.Skip(1).ToList());
                    Console.Write(converted);
                    return converted.Length;
                }
                return 0;
            }

            if (name == "sprintf")
            {
                if (args.Count > 1 && args[0] is PointerValue dest && args[1] is string fmt)
                {
                    var converted = ConvertPrintfFormat(fmt, args.Skip(2).ToList());
                    dest.Set(converted);
                    return converted.Length;
                }
                return 0;
            }

            if (_functions.TryGetValue(name, out var func))
            {
                return EvaluateFunction(func, args) ?? 0;
            }

            throw new NotImplementedException($"function {name}");
        }

        private static string ConvertPrintfFormat(string fmt, List<object> args)
        {
            fmt = fmt.Replace("\\n", "\n");
            var sb = new System.Text.StringBuilder();
            int argIdx = 0;
            for (int i = 0; i < fmt.Length; i++)
            {
                if (fmt[i] == '%' && i + 1 < fmt.Length)
                {
                    char spec = fmt[i + 1];
                    if (spec == 'd' || spec == 's')
                    {
                        var val = argIdx < args.Count ? args[argIdx++] : null;
                        sb.Append(val?.ToString() ?? string.Empty);
                        i++; // spec文字を飛ばす
                        continue;
                    }
                }
                sb.Append(fmt[i]);
            }
            return sb.ToString();
        }
    }
}

