using System;
using Amazon.Glacier.Transfer;
using Microsoft.Extensions.DependencyInjection;
using GlacierBackup.FileSearchers;
using GlacierBackup.Writers;

namespace GlacierBackup;

public static class IServiceCollectionExtensions
{
    public static IServiceCollection AddGlacierBackupServices(this IServiceCollection services, Options opts)
    {
        services
            .AddSingleton(opts)
            .AddFileSearcher(opts)
            .AddResultWriter(opts)
            .AddSingleton(s => new ArchiveTransferManager(opts.Credentials, opts.Region));

        return services;
    }

    static IServiceCollection AddFileSearcher(this IServiceCollection services, Options opts)
    {
        switch(opts.BackupType)
        {
            case BackupType.Assets:
                if (string.Equals(opts.VaultName, "photos", StringComparison.OrdinalIgnoreCase))
                {
                    services.AddSingleton<IFileSearcher, PhotoAssetFileSearcher>();
                }
                else if (string.Equals(opts.VaultName, "videos", StringComparison.OrdinalIgnoreCase))
                {
                    services.AddSingleton<IFileSearcher, VideoAssetFileSearcher>();
                }
                else
                {
                    throw new ApplicationException($"Unknown Asset Type for vault {opts.VaultName}");
                }

                break;
            case BackupType.Full:
                services.AddSingleton<IFileSearcher, AllFileSearcher>();
                break;
            case BackupType.File:
                services.AddSingleton<IFileSearcher, SingleFileSearcher>();
                break;
            case BackupType.List:
                services.AddSingleton<IFileSearcher, ListFileSearcher>();
                break;
            default:
                throw new InvalidOperationException("This backup type is not supported");
        }

        return services;
    }

    static IServiceCollection AddResultWriter(this IServiceCollection services, Options opts)
    {
        switch(opts.OutputType)
        {
            case OutputType.PhotoSql:
                services.AddSingleton<IResultWriter>(s => new PhotosPgSqlResultWriter(opts.Output));
                break;
            case OutputType.VideoSql:
                services.AddSingleton<IResultWriter>(s => new VideosPgSqlResultWriter(opts.Output));
                break;
            case OutputType.Csv:
                services.AddSingleton<IResultWriter>(s => new CsvResultWriter(opts.Output));
                break;
            default:
                throw new InvalidOperationException("This output type is not properly supported");
        };

        return services;
    }
}
