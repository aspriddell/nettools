using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.Extensions.Logging;

namespace NetTools.Components;

public partial class FileSubmissionComponent<TItem, TOut> : ComponentBase
{
    [Parameter]
    public TOut Current { get; set; }
    
    [Parameter]
    public EventCallback<TOut> CurrentChanged { get; set; }
    
    [Parameter]
    public Func<IReadOnlyCollection<TItem>, TOut> ProcessItems { get; set; }
    
    [Inject]
    private ILogger<FileSubmissionComponent<TItem, TOut>> Logger { get; set; }
    
    private string CurrentFileName { get; set; }
    private bool ShowUploadDialog { get; set; }
    private bool UploadedFileFailed { get; set; }

    private CancellationTokenSource FileProcessing { get; set; }

    private async Task FileSubmitted(InputFileChangeEventArgs obj)
    {
        UploadedFileFailed = false;
        
        if (FileProcessing?.IsCancellationRequested != false)
        {
            FileProcessing?.Dispose();
            FileProcessing = new CancellationTokenSource();
        }

        ShowUploadDialog = false;
        
        IReadOnlyCollection<TItem> results;
        Logger.LogInformation("Processing file {type}", obj.File.ContentType);
        
        switch (obj.File.ContentType)
        {
            case "text/json":
            case "application/json":
            {
                await using var stream = obj.File.OpenReadStream();
                var result = await JsonSerializer.DeserializeAsync<TItem>(stream, Program.JsonOptions);
                results = [result];

                break;
            }

            case "application/zip":
            case "application/x-zip":
            case "application/x-zip-compressed":
            {
                var memoryStream = new MemoryStream();
                await using (var file = obj.File.OpenReadStream())
                {
                    await file.CopyToAsync(memoryStream).ConfigureAwait(false);
                    memoryStream.Seek(0, SeekOrigin.Begin);
                }

                using var archive = new ZipArchive(memoryStream, ZipArchiveMode.Read, false);
                var archiveResults = new List<TItem>(archive.Entries.Count);

                foreach (var entry in archive.Entries.Where(x => Path.GetExtension(x.Name) == ".json"))
                {
                    if (FileProcessing.IsCancellationRequested)
                    {
                        return;
                    }
                    
                    await using var entryStream = entry.Open();

                    try
                    {
                        var result = await JsonSerializer.DeserializeAsync<TItem>(entryStream, Program.JsonOptions);
                        archiveResults.Add(result);
                    }
                    catch (Exception e)
                    {
                        Logger.LogWarning(e, "Failed to process {FileName}: {Error}", entry.FullName, e.Message);
                    }
                }

                results = archiveResults;
                break;
            }

            default:
                return;
        }

        try
        {
            // can't reload ui until file processing has finished
            await InvokeAsync(StateHasChanged);

            CurrentFileName = obj.File.Name;
            Current = ProcessItems.Invoke(results);

            await CurrentChanged.InvokeAsync(Current);
        }
        catch (Exception e)
        {
            UploadedFileFailed = true;
            Logger.LogError(e, "Failed to process files: {Error}", e.Message);
        }
        finally
        {
            FileProcessing?.Dispose();
            FileProcessing = null;
        }
    }
}