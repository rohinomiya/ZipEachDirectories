using System.IO.Compression;

// コマンドラインオプション検証
if (args.Length == 0)
{
    Console.Error.WriteLine("警告: 作業ディレクトリが指定されていません");
    Console.Error.WriteLine("使い方: ZipEachDirectories <作業ディレクトリパス>");
    Environment.Exit(1);
}

string workingDirectory = args[0];

// 作業ディレクトリの存在確認
if (!Directory.Exists(workingDirectory))
{
    Console.Error.WriteLine($"エラー: 指定されたディレクトリが見つかりません: {workingDirectory}");
    Environment.Exit(1);
}

// 作業ディレクトリの絶対パスを取得
string absolutePath = Path.GetFullPath(workingDirectory);

// システムディレクトリの判定
if (IsSystemDirectory(absolutePath))
{
    Console.Error.WriteLine($"警告: 指定されたディレクトリはシステムが使用しているディレクトリです: {absolutePath}");
    Environment.Exit(1);
}

// 作業ディレクトリ直下の各ディレクトリを処理
try
{
    var directories = Directory.EnumerateDirectories(absolutePath, "*", SearchOption.TopDirectoryOnly).ToList();

    if (directories.Count == 0)
    {
        Console.WriteLine($"情報: 処理するディレクトリがありません: {absolutePath}");
        Environment.Exit(0);
    }

    int successCount = 0;
    int failureCount = 0;

    foreach (string dirPath in directories)
    {
        string dirName = Path.GetFileName(dirPath);
        string zipFileName = $"{dirName}.zip";
        string zipFilePath = Path.Combine(absolutePath, zipFileName);

        try
        {
            Console.WriteLine($"処理中: {dirName}");

            // 既存のzipファイルがある場合は削除
            if (File.Exists(zipFilePath))
            {
                File.Delete(zipFilePath);
                Console.WriteLine($"  既存のzipファイルを削除: {zipFileName}");
            }

            // ディレクトリ内のファイルをzipに圧縮（ディレクトリ構造なし）
            using (var archive = ZipFile.Open(zipFilePath, ZipArchiveMode.Create))
            {
                var files = Directory.EnumerateFiles(dirPath, "*", SearchOption.AllDirectories);

                foreach (string filePath in files)
                {
                    string entryName = GetEntryName(filePath, dirPath);
                    archive.CreateEntryFromFile(filePath, entryName, CompressionLevel.Optimal);
                }
            }

            Console.WriteLine($"  圧縮完了: {zipFileName}");

            // ディレクトリを削除
            Directory.Delete(dirPath, true);
            Console.WriteLine($"  ディレクトリを削除: {dirName}");
            successCount++;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"  エラー: {dirName} の処理に失敗しました: {ex.Message}");
            failureCount++;
        }
    }

    Console.WriteLine($"\n処理完了: 成功 {successCount}, 失敗 {failureCount}");
    Environment.Exit(failureCount > 0 ? 1 : 0);
}
catch (Exception ex)
{
    Console.Error.WriteLine($"予期しないエラーが発生しました: {ex.Message}");
    Environment.Exit(1);
}

/// <summary>
/// システムディレクトリであるかを判定します
/// </summary>
static bool IsSystemDirectory(string path)
{
    var systemPaths = new[]
    {
        Environment.GetFolderPath(Environment.SpecialFolder.System),
        Environment.GetFolderPath(Environment.SpecialFolder.Windows),
        Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles),
        Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86),
        Environment.GetFolderPath(Environment.SpecialFolder.CommonProgramFiles),
        Environment.GetFolderPath(Environment.SpecialFolder.CommonProgramFilesX86),
    };

    string normalizedPath = Path.GetFullPath(path).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);

    foreach (var systemPath in systemPaths)
    {
        string normalizedSystemPath = Path.GetFullPath(systemPath).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);

        if (normalizedPath.Equals(normalizedSystemPath, StringComparison.OrdinalIgnoreCase) ||
            normalizedPath.StartsWith(normalizedSystemPath + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }
    }

    return false;
}

/// <summary>
/// サブディレクトリなしのエントリ名を取得します
/// </summary>
static string GetEntryName(string filePath, string baseDirectoryPath)
{
    // ファイルパスから基本ディレクトリを除去
    string relativePath = Path.GetRelativePath(baseDirectoryPath, filePath);

    // バックスラッシュをスラッシュに統一（zipファイル内の標準）
    return relativePath.Replace(Path.DirectorySeparatorChar, '/');
}
