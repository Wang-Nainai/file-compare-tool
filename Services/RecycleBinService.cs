using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace FileCompareTool.Services;

public class DeleteResult
{
    public int SuccessCount { get; set; }
    public int FailCount { get; set; }
    public List<string> FailedFiles { get; set; } = new();
    public List<string> FailedReasons { get; set; } = new();
}

public static class RecycleBinService
{
    public static async Task<DeleteResult> DeleteFilesAsync(
        IEnumerable<string> filePaths,
        Func<string, bool> pathValidator,
        IProgress<(int current, int total, string currentFile)>? progress = null)
    {
        var result = new DeleteResult();
        var files = filePaths
            .Where(p => !string.IsNullOrWhiteSpace(p))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        var total = files.Count;
        var current = 0;

        foreach (var filePath in files)
        {
            current++;
            progress?.Report((current, total, filePath));

            try
            {
                if (!pathValidator(filePath))
                {
                    AddFailure(result, filePath, LocalizationService.T("Delete_ReasonPathValidation"));
                    continue;
                }

                if (!File.Exists(filePath))
                {
                    AddFailure(result, filePath, LocalizationService.T("Delete_ReasonFileMissing"));
                    continue;
                }

                var fullPath = Path.GetFullPath(filePath);
                if (!pathValidator(fullPath))
                {
                    AddFailure(result, filePath, LocalizationService.T("Delete_ReasonPathMismatch"));
                    continue;
                }

                await Task.Run(() => MoveToRecycleBin(fullPath));
                result.SuccessCount++;
            }
            catch (Exception ex)
            {
                AddFailure(result, filePath, ex.Message);
            }
        }

        return result;
    }

    private static void AddFailure(DeleteResult result, string filePath, string reason)
    {
        result.FailCount++;
        result.FailedFiles.Add(filePath);
        result.FailedReasons.Add(reason);
    }

    private static void MoveToRecycleBin(string filePath)
    {
        var fileOp = new SHFILEOPSTRUCT
        {
            wFunc = FO_DELETE,
            pFrom = filePath + '\0' + '\0',
            fFlags = FOF_ALLOWUNDO | FOF_NOCONFIRMATION | FOF_NOERRORUI | FOF_SILENT
                     | FOF_WANTNUKEWARNING
        };

        var result = SHFileOperation(ref fileOp);
        if (result != 0)
            throw new IOException(LocalizationService.Format("Delete_ErrorRecycleCodeFormat", result));

        if (fileOp.fAnyOperationsAborted)
            throw new IOException(LocalizationService.T("Delete_ErrorCancelled"));

        // SHFileOperation 返回 0 但文件仍在时，说明没有真正移动成功
        if (File.Exists(filePath))
            throw new IOException(LocalizationService.T("Delete_ErrorNotMoved"));
    }

    private const uint FO_DELETE = 3;
    private const ushort FOF_SILENT = 0x0004;
    private const ushort FOF_NOCONFIRMATION = 0x0010;
    private const ushort FOF_ALLOWUNDO = 0x0040;
    private const ushort FOF_NOERRORUI = 0x0400;
    private const ushort FOF_WANTNUKEWARNING = 0x4000;

    [DllImport("shell32.dll", CharSet = CharSet.Unicode)]
    private static extern int SHFileOperation(ref SHFILEOPSTRUCT lpFileOp);

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    private struct SHFILEOPSTRUCT
    {
        public IntPtr hwnd;
        public uint wFunc;
        [MarshalAs(UnmanagedType.LPWStr)]
        public string pFrom;
        [MarshalAs(UnmanagedType.LPWStr)]
        public string? pTo;
        public ushort fFlags;
        [MarshalAs(UnmanagedType.Bool)]
        public bool fAnyOperationsAborted;
        public IntPtr hNameMappings;
        [MarshalAs(UnmanagedType.LPWStr)]
        public string? lpszProgressTitle;
    }
}
