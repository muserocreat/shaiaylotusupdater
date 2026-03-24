using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Windows;
using Parsec.Shaiya.Data;
using Updater.Common;
using Updater.Configuration;
using Updater.Core;
using Updater.Data;
using Updater.Extensions;
using Updater.Helpers;
using Updater.Resources;

namespace Updater
{
    public static class Program
    {
        public static void DoWork(HttpClient httpClient, BackgroundWorker backgroundWorker)
        {
            ClientConfiguration? clientConfiguration = null;
            ServerConfiguration? serverConfiguration = null;

            try
            {
                serverConfiguration = new ServerConfiguration(httpClient);
                clientConfiguration = new ClientConfiguration();

                if (serverConfiguration.UpdaterVersion > Constants.UpdaterVersion)
                {
                    backgroundWorker.ReportProgress(0, Strings.UserState1);
                    UpdaterPatcher(httpClient, backgroundWorker);
                    return;
                }

                if (serverConfiguration.PatchFileVersion > clientConfiguration.CurrentVersion)
                {
                    backgroundWorker.ReportProgress(0, new ProgressReport("ProgressBar1"));
                    backgroundWorker.ReportProgress(0, new ProgressReport("ProgressBar2"));

                    int progressMax = serverConfiguration.PatchFileVersion - clientConfiguration.CurrentVersion;
                    int progressValue = 1;
                    bool needsRebuild = false;

                    // Parse lotu.sah/saf once before the patch loop
                    Parsec.Shaiya.Data.Data? gameData = null;
                    if (File.Exists("lotu.sah") && File.Exists("lotu.saf"))
                        gameData = new Parsec.Shaiya.Data.Data("lotu.sah", "lotu.saf");

                    // Prefetch: start downloading the first patch
                    var nextPatch = new Patch(clientConfiguration.CurrentVersion + 1);
                    Task<byte[]?>? prefetchTask = PrefetchPatch(httpClient, nextPatch);

                    while (clientConfiguration.CurrentVersion < serverConfiguration.PatchFileVersion)
                    {
                        var progressText = string.Format(Strings.UserState2, progressValue, progressMax);
                        backgroundWorker.ReportProgress(0, progressText);

                        var patch = nextPatch;

                        // Wait for the prefetched download
                        byte[]? patchBytes = prefetchTask?.Result;
                        if (patchBytes != null && patchBytes.Length > 0)
                            File.WriteAllBytes(patch.Path, patchBytes);
                        else
                            httpClient.DownloadFile(patch.Url, patch.Path);

                        if (!File.Exists(patch.Path))
                        {
                            backgroundWorker.ReportProgress(0, Strings.UserState3);
                            gameData?.Sah.Write(gameData.Sah.Path);
                            return;
                        }

                        // Prefetch next patch while we extract and apply current one
                        bool hasNextPatch = clientConfiguration.CurrentVersion + 2 <= serverConfiguration.PatchFileVersion;
                        if (hasNextPatch)
                        {
                            nextPatch = new Patch(clientConfiguration.CurrentVersion + 2);
                            prefetchTask = PrefetchPatch(httpClient, nextPatch);
                        }
                        else
                        {
                            prefetchTask = null;
                        }

                        backgroundWorker.ReportProgress(0, Strings.UserState4);

                        if (!patch.ExtractToCurrentDirectory())
                        {
                            backgroundWorker.ReportProgress(0, Strings.UserState5);
                            gameData?.Sah.Write(gameData.Sah.Path);
                            return;
                        }

                        File.Delete(patch.Path);
                        backgroundWorker.ReportProgress(0, Strings.UserState6);
                        needsRebuild |= DataPatcher(backgroundWorker, ref gameData);
                        
                        // [CRITICAL FIX] Transacción Asegurada: 
                        // Escribir cabecera .sah y guardar versión inmediatamente para evitar corrupción
                        // progresiva del .saf y pérdida de progreso en caso de crasheo.
                        gameData?.Sah.Write(gameData.Sah.Path);

                        clientConfiguration.CurrentVersion++;
                        clientConfiguration.Save();

                        progressValue++;

                        var progressPercentage = MathHelper.Percentage(clientConfiguration.CurrentVersion, serverConfiguration.PatchFileVersion);
                        if (progressPercentage > 0)
                        {
                            backgroundWorker.ReportProgress(progressPercentage, new ProgressReport("ProgressBar2"));
                        }
                    }

                    // Write lotu.sah once after all patches
                    gameData?.Sah.Write(gameData.Sah.Path);

                    // Only rebuild archive if files were deleted (compaction needed)
                    if (needsRebuild)
                    {
                        backgroundWorker.ReportProgress(0, Strings.UserState7);
                        DataBuilder(backgroundWorker);
                    }

                    backgroundWorker.ReportProgress(0, Strings.UserState8);
                }
                else
                {
                    // Client is up to date
                    var formattedVersion = (serverConfiguration.PatchFileVersion / 100.0).ToString("0.00");
                    backgroundWorker.ReportProgress(0, $"Tu cliente de Shaiya Lotus está actualizado a la última versión (v{formattedVersion}).");
                    backgroundWorker.ReportProgress(100, new ProgressReport("ProgressBar1"));
                    backgroundWorker.ReportProgress(100, new ProgressReport("ProgressBar2"));
                }
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show(ex.ToString(), "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                clientConfiguration?.Save();
                clientConfiguration?.Dispose();
                clientConfiguration = null;

                serverConfiguration?.Dispose();
                serverConfiguration = null;
            }
        }

        private static void DataBuilder(BackgroundWorker backgroundWorker)
        {
            if (!File.Exists("lotu.sah") || !File.Exists("lotu.saf"))
                return;

            var data = new Parsec.Shaiya.Data.Data("lotu.sah", "lotu.saf");
            File.Move("lotu.sah", "lotu.sah.bak", true);
            File.Move("lotu.saf", "lotu.saf.bak", true);
            data.Sah.Path += ".bak";
            data.Saf.Path += ".bak";

            var progressReport = new ProgressReport("ProgressBar1");
            var progress = new Progress(backgroundWorker, progressReport, data.FileIndex.Count, 1);
            Updater.Data.DataBuilder.Build(data, Directory.GetCurrentDirectory(), progress.PerformStep);

            File.Delete(data.Sah.Path);
            File.Delete(data.Saf.Path);
        }

        private static bool DataPatcher(BackgroundWorker backgroundWorker, ref Parsec.Shaiya.Data.Data? gameData)
        {
            bool hadDeletions = false;

            if (File.Exists("delete.lst"))
            {
                hadDeletions = true;

                if (gameData == null && File.Exists("lotu.sah") && File.Exists("lotu.saf"))
                    gameData = new Parsec.Shaiya.Data.Data("lotu.sah", "lotu.saf");

                if (gameData != null)
                {
                    var paths = File.ReadAllLines("delete.lst");

                    var progressReport = new ProgressReport("ProgressBar1");
                    var progress = new Progress(backgroundWorker, progressReport, paths.Length, 1);

                    gameData.RemoveFilesFromLst("delete.lst", progress.PerformStep);
                }

                File.Delete("delete.lst");
            }

            if (File.Exists("update.sah") && File.Exists("update.saf"))
            {
                if (gameData == null && File.Exists("lotu.sah") && File.Exists("lotu.saf"))
                    gameData = new Parsec.Shaiya.Data.Data("lotu.sah", "lotu.saf");

                if (gameData != null)
                {
                    var update = new Parsec.Shaiya.Data.Data("update.sah", "update.saf");

                    var progressReport = new ProgressReport("ProgressBar1");
                    var progress = new Progress(backgroundWorker, progressReport, update.FileIndex.Count, 1);

                    using (var dataPatcher = new DataPatcher())
                        dataPatcher.Patch(gameData, update, progress.PerformStep);
                }

                File.Delete("update.sah");
                File.Delete("update.saf");
            }

            return hadDeletions;
        }

        private static Task<byte[]?> PrefetchPatch(HttpClient httpClient, Patch patch)
        {
            return Task.Run(() =>
            {
                try
                {
                    return httpClient.GetByteArrayAsync(patch.Url).Result;
                }
                catch
                {
                    return null;
                }
            });
        }

        /// <param name="httpClient"></param>
        /// <param name="backgroundWorker"></param>
        private static void UpdaterPatcher(HttpClient httpClient, BackgroundWorker backgroundWorker)
        {
            var newUpdater = new NewUpdater();
            
            httpClient.DownloadFile(newUpdater.Url, newUpdater.Path, (downloaded, total) =>
            {
                var percentage = (int)((downloaded * 100) / total);
                backgroundWorker.ReportProgress(percentage, new ProgressReport("ProgressBar1"));
            });

            if (!File.Exists(newUpdater.Path))
                return;

            var fileName = Path.Combine(Directory.GetCurrentDirectory(), "game.exe");
            Process.Start(fileName, "new updater");

            var currentProcess = Process.GetCurrentProcess();
            currentProcess.Kill();
        }
    }
}
