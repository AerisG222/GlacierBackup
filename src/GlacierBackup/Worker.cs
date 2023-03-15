using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Amazon.Glacier.Transfer;
using GlacierBackup.FileSearchers;
using GlacierBackup.Writers;
using Microsoft.Extensions.Hosting;

namespace GlacierBackup;

public class Worker
    : BackgroundService
{
    const int RETRY_COUNT = 3;
    const int START_WAIT_TIME_MS = 5000;

    readonly Options _opts;
    readonly IResultWriter _resultWriter;
    readonly IFileSearcher _searcher;
    readonly ArchiveTransferManager _atm;
    readonly IHostApplicationLifetime _appLifetime;

    public Worker(
        IHostApplicationLifetime appLifetime,
        Options opts,
        IFileSearcher searcher,
        IResultWriter writer,
        ArchiveTransferManager atm
    ) {
        _appLifetime = appLifetime ?? throw new ArgumentNullException(nameof(appLifetime));
        _opts = opts ?? throw new ArgumentNullException(nameof(opts));
        _searcher = searcher ?? throw new ArgumentNullException(nameof(searcher));
        _resultWriter = writer ?? throw new ArgumentNullException(nameof(writer));
        _atm = atm ?? throw new ArgumentNullException(nameof(atm));
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await BackupFilesAsync(stoppingToken);

        _appLifetime.StopApplication();
    }

    async Task BackupFilesAsync(CancellationToken stoppingToken)
    {
        var files = _searcher.FindFiles(_opts.BackupSource).ToArray();
        var parallelOpts = new ParallelOptions
        {
            CancellationToken = stoppingToken,
            MaxDegreeOfParallelism = Math.Max(Environment.ProcessorCount - 1, 1)
        };

        var results = new BackupResult[files.Length];

        await Parallel.ForEachAsync(Enumerable.Range(0, files.Length), parallelOpts, async (index, token) =>
        {
            var file = files[index];

            results[index] = await BackupFileAsync(file, stoppingToken);
        });

        _resultWriter.WriteResults(results);
    }

    async Task<BackupResult> BackupFileAsync(string file, CancellationToken stoppingToken)
    {
        var backupFile = new BackupFile(file, _opts.RelativeRoot);

        var result = new BackupResult
        {
            Region = _opts.Region,
            Vault = _opts.VaultName,
            Backup = backupFile
        };

        for (var i = 1; i <= RETRY_COUNT; i++)
        {
            var attempt = i > 1 ? $" (attempt {i})" : string.Empty;

            try
            {
                Console.WriteLine($"  - backing up {backupFile.GlacierDescription}{attempt}");

                result.Result = await _atm.UploadAsync(_opts.VaultName, backupFile.GlacierDescription, backupFile.FullPath);

                return result;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"  - error backing up {backupFile.GlacierDescription}{attempt}: {ex.Message}");

                await Task.Delay(START_WAIT_TIME_MS * i, stoppingToken);
            }
        }

        Console.WriteLine($" ** unable to backup {backupFile.GlacierDescription} **");

        return result;
    }
}
