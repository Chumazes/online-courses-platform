using System.Diagnostics;
using System.IO;
using System.Text.Json;
using Microsoft.Win32;
using OnlineCourses.Client.Api;

namespace OnlineCourses.Desktop.Infrastructure;

public static class FileActionHelper
{
    public static string? TryOpen(string? downloadUrl)
    {
        if (string.IsNullOrWhiteSpace(downloadUrl))
        {
            return "Файл пока недоступен.";
        }

        try
        {
            Process.Start(new ProcessStartInfo(downloadUrl)
            {
                UseShellExecute = true
            });

            return null;
        }
        catch
        {
            return "Не удалось открыть файл. Проверь, что он ещё доступен и для этого формата есть приложение.";
        }
    }

    public static async Task<(bool Success, string? SavedPath, string? ErrorMessage)> TryDownloadAsync(
        FilesClient filesClient,
        string? fileUrl,
        string? suggestedFileName,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(fileUrl))
        {
            return (false, null, "Файл пока недоступен для скачивания.");
        }

        var dialog = new SaveFileDialog
        {
            Title = "Сохранить файл урока",
            FileName = ResolveFileName(fileUrl, suggestedFileName),
            Filter = "Все файлы|*.*"
        };

        if (dialog.ShowDialog() != true)
        {
            return (false, null, null);
        }

        try
        {
            await filesClient.DownloadAsync(fileUrl, dialog.FileName, cancellationToken);
            return (true, dialog.FileName, null);
        }
        catch (ApiException ex)
        {
            return (false, null, TryExtractApiMessage(ex.ResponseBody) ?? "Не удалось скачать файл урока.");
        }
        catch
        {
            return (false, null, "Не удалось скачать файл урока. Проверь, что файл ещё существует и папка доступна для записи.");
        }
    }

    private static string ResolveFileName(string fileUrl, string? suggestedFileName)
    {
        if (!string.IsNullOrWhiteSpace(suggestedFileName))
        {
            return suggestedFileName!;
        }

        try
        {
            return Path.GetFileName(fileUrl);
        }
        catch
        {
            return "lesson-file";
        }
    }

    private static string? TryExtractApiMessage(string? responseBody)
    {
        if (string.IsNullOrWhiteSpace(responseBody))
        {
            return null;
        }

        try
        {
            using var document = JsonDocument.Parse(responseBody);
            if (document.RootElement.ValueKind == JsonValueKind.Object &&
                document.RootElement.TryGetProperty("message", out var messageElement))
            {
                var message = messageElement.GetString();
                if (!string.IsNullOrWhiteSpace(message))
                {
                    return message;
                }
            }
        }
        catch
        {
        }

        return null;
    }
}
