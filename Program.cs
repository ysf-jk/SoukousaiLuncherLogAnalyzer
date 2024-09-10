using System.Security.Cryptography;
using System.Text;

namespace SoukousaiLuncherLogAnalyzer
{
    internal class Program
    {
        static List<string> ContainsLogsDirectoryPath = new List<string>();
        static List<string> LogDirectoryPath = new List<string>();

        class GameLog
        {
            public string Name;
            public int StartTime;
            public int? ExitTime = null;

            public GameLog (string Name, int StartTime)
            {
                this.Name = Name;
                this.StartTime = StartTime;
            }
        }
        static List<GameLog> Logs = new List<GameLog>();

        static List<string> GameNames = new List<string>();

        static string Result = "";

        static bool StopWhenExitTimeNull = true;

        static void Main(string[] args)
        {
            Console.OutputEncoding = Encoding.GetEncoding("utf-8");
            if (args.Length == 0)
            {
                while (ContainsLogsDirectoryPath.Count == 0)
                {
                    List<string> InvalidDirectory = new List<string>();
                    Console.WriteLine("Logが含まれているパスを入力(複数ある場合は,で区切る):");
                    ContainsLogsDirectoryPath = Console.ReadLine().Replace("\\", "/").Replace("file:///", "").Split(',').ToList();

                    if (ContainsLogsDirectoryPath.Count > 1)
                    {
                        foreach (string i in args)
                        {
                            if (Directory.Exists(i.Replace("\\", "/").Replace("file:///", "")))
                            {
                                ContainsLogsDirectoryPath.Add(i.Replace("\\", "/").Replace("file:///", ""));
                            }
                            else
                            {
                                InvalidDirectory.Add(i.Replace("\\", "/").Replace("file:///", ""));
                            }
                        }
                        if (InvalidDirectory.Count > 0)
                        {
                            Console.WriteLine($"一部のパス(\"{string.Join("\", \"", InvalidDirectory)}\")が無効です。\n有効なパス(\"{string.Join("\", \"", ContainsLogsDirectoryPath)}\")のみを扱いますがよろしいですか？");
                            Console.Write("[y/n]");
                            if (Console.ReadLine().Replace(" ", "") == "y")
                            {
                                Console.WriteLine("続行します。");
                                break;
                            }
                            else
                            {
                                ContainsLogsDirectoryPath = null;
                                InvalidDirectory.Clear();
                            }
                        }
                    }
                    else if (!Directory.Exists(ContainsLogsDirectoryPath[^1]))
                    {
                        Console.WriteLine("無効なパスです。");
                        ContainsLogsDirectoryPath = null;
                    }
                }
            }
            else
            {
                List<string> InvalidDirectory = new List<string>();
                foreach (string i in args)
                {
                    if (Directory.Exists(i.Replace("\\", "/").Replace("file:///", "")))
                    {
                        ContainsLogsDirectoryPath.Add(i.Replace("\\", "/").Replace("file:///", ""));
                    }
                    else
                    {
                        InvalidDirectory.Add(i.Replace("\\", "/").Replace("file:///", ""));
                    }
                }
                if (InvalidDirectory.Count > 0)
                    Console.WriteLine($"一部のパス(\"{string.Join("\", \"", InvalidDirectory)}\")が無効です。\n有効なパス(\"{string.Join("\", \"", ContainsLogsDirectoryPath)}\")のみを扱います。");
            }

            Analyaize(ContainsLogsDirectoryPath.ToArray());
        }

        static void Analyaize(string[] Patha)
        {
            foreach (string i in ContainsLogsDirectoryPath)//実際のログファイルだけをとりだす
            {
                {
                    var y = Directory.GetFiles(i, "*.log", SearchOption.AllDirectories).Where(x =>
                    {
                        using (var sr = new StreamReader(x))
                        {
                            var r = sr.ReadLine();
                            if (r == "Log By SoukousaiLuncher made by YSFJK.")
                                return true;
                            else
                                return false;
                        }
                    });
                    foreach (string j in y)
                    {
                        LogDirectoryPath.Add(j);
                    }
                }
            }


            foreach (string i in LogDirectoryPath)
            {
                Console.WriteLine($"Reading:{i}");
                using (var sr = new StreamReader(i))
                {
                    while (sr.Peek() > -1)
                    {
                        string t = sr.ReadLine();
                        if (t.Contains("を起動します"))
                        {
                            Logs.Add(new GameLog(t.Split(':')[1].Split("を起動します")[0], int.Parse(t.Split(':')[0])));
                            if (!GameNames.Contains(t.Split(':')[1].Split("を起動します")[0]))
                                GameNames.Add(t.Split(':')[1].Split("を起動します")[0]);
                        }
                        else if (t.Contains("を終了できませんでした") || t.Contains("ゲームを終了しました"))
                        {
                            Logs[^1].ExitTime = int.Parse(t.Split(":")[0]);
                        }
                    }
                }

                if (Logs.Where(x => x.ExitTime == null).ToArray().Length != 0)
                {
                    if (StopWhenExitTimeNull) {
                        Console.WriteLine($"終了されていないゲームを検知しました。ログディレクトリ:{i}");
                        string answ = "";
                        while (answ != "y" && answ != "n")
                        {
                            Console.Write("今後も終了されていないゲームを検知した場合、一時停止しますか？[y/n]");
                            answ = Console.ReadLine();
                        }
                        if (answ == "n")
                            StopWhenExitTimeNull = false;
                    }
                    else
                    {
                        Console.WriteLine($"終了されていないゲームを検知しました。ログディレクトリ:{i}");
                    }
                }
                
                foreach(string j in GameNames)
                {
                    Console.Write($"{j}:\t");
                    Console.Write($"実行回数:{Logs.Where(x => x.Name == j).ToArray().Length}\t");
                    Console.Write($"総実行時間:{Logs.Where(x => x.Name == j && x.ExitTime != null).Select(x => x.ExitTime - x.StartTime).Sum()}\n");
                    Console.WriteLine();
                }
            }
            Console.WriteLine();

            Result += "\t,総実行回数,総実行時間,1分以上実行回数,1分以上実行時間,ExitTime=nullの回数\n";
            foreach (string i in GameNames)
            {
                Result += $"{i}," +
                    $"{Logs.Where(x => x.Name == i).ToArray().Length},{Logs.Where(x => x.Name == i && x.ExitTime != null).Select(x => x.ExitTime - x.StartTime).Sum()}s," +
                    $"{Logs.Where(x => x.Name == i && x.ExitTime != null).Where(x => (x.ExitTime - x.StartTime) >= 60).ToArray().Length},{Logs.Where(x => x.Name == i && x.ExitTime != null).Where(x => (x.ExitTime - x.StartTime) >= 60).Select(x => x.ExitTime - x.StartTime).ToArray().Sum()}s," +
                    $"{Logs.Where(x => x.Name == i && x.ExitTime == null).ToArray().Length}\n\n";
            }

            Console.WriteLine("集計結果:");
            Console.WriteLine(Result.Replace(",", "\t|"));


            Console.Write("結果を保存しますか？[y/n]");
            string ans = "";
            while (ans != "y" && ans != "n")
            {
                ans = Console.ReadLine();
            }
            if (ans == "y")
            {
                bool ReadyToSave = false;
                string SaveTo = "";
                string FileName = "";
                while (!ReadyToSave)
                {
                    SaveTo = "";
                    while (!Directory.Exists(SaveTo))
                    {
                        Console.WriteLine("保存するディレクトリを指定してください:");
                        SaveTo = Console.ReadLine().Replace("\\", "/");
                    }
                    if (SaveTo[^1] == '/')
                        SaveTo = SaveTo[0..^2];


                    FileName = "";
                    Console.Write("File Name:");
                    FileName = Console.ReadLine();

                    Console.WriteLine($"\"{SaveTo}/{FileName}.csv\"として保存します。");
                    Console.Write("確認[y/n]");
                    ReadyToSave = Console.ReadLine() == "y";
                }

                foreach (string i in GameNames)//詳細情報
                {
                    Result += $"\n" +
                        $"{i}\n" +
                        $"開始時刻,終了時刻,実行時間\n";
                    foreach (var j in Logs.Where(x => x.Name == i).ToArray())
                    {
                        Result += $"{DateTimeOffset.FromUnixTimeSeconds(j.StartTime).LocalDateTime.ToString()},{(j.ExitTime == null ? "NoData" : DateTimeOffset.FromUnixTimeSeconds(j.ExitTime.Value).LocalDateTime.ToString())},{j.ExitTime - j.StartTime}s\n";
                    }
                    Result += "\n";
                }

                using (var sw = new StreamWriter($"{SaveTo}/{FileName}.csv"))
                {
                    sw.Write(Result.Replace("\t", "").Replace("\n\n", "\n"));
                }
            }
        }
    }
}
