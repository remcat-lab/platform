using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Hosting;
using System.Collections.Generic;
using System.IO.Compression;
using System.IO;
using System.Threading.Tasks;
using System.Text.Json;
using System.Linq;

var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

app.MapPost("/apis/api/check_csv", async (HttpRequest request) =>
{
    // multipart/form-data 파싱: "csv" 파일 필드 가져오기
    if (!request.HasFormContentType)
        return Results.BadRequest("Expect multipart/form-data");

    var form = await request.ReadFormAsync();
    var file = form.Files.GetFile("csv");

    if (file == null)
        return Results.BadRequest("Missing 'csv' file");

    List<string> neededFiles = new();

    // CSV 파싱: 간단히 첫 줄 스킵, path 컬럼만 추출
    using (var stream = file.OpenReadStream())
    using (var reader = new StreamReader(stream))
    {
        string? line = await reader.ReadLineAsync(); // header
        while ((line = await reader.ReadLineAsync()) != null)
        {
            var parts = line.Split(',');
            if (parts.Length < 1)
                continue;
            var path = parts[0];

            // 예시: 임의 조건 - 파일명에 "upload" 포함하는 파일만 필요하다고 판단
            if (path.Contains("upload"))
                neededFiles.Add(path);
        }
    }

    var responseObj = new { needed_files = neededFiles };

    return Results.Json(responseObj);
});

app.MapPost("/apis/api/upload", async (HttpRequest request) =>
{
    // 요청 바디가 tar+brotli 압축된 스트림이라 가정
    // 압축 해제 후 임시 폴더에 파일 추출

    // 임시 폴더 준비
    var tempDir = Path.Combine(Path.GetTempPath(), "upload_extract");
    Directory.CreateDirectory(tempDir);

    try
    {
        // brotli 스트림 해제
        using var brotliStream = new System.IO.Compression.BrotliStream(request.Body, CompressionMode.Decompress);

        // tar 아카이브 해제
        using var tarArchive = TarArchive.Open(brotliStream);

        // TarArchive는 System.Formats.Tar (net7+) 또는 타사 라이브러리 필요
        // 여기서는 net7+ System.Formats.Tar 사용 예시
        using var tarReader = new System.Formats.Tar.TarReader(brotliStream);

        while (true)
        {
            var entry = tarReader.GetNextEntry();
            if (entry == null)
                break;

            if (entry.EntryType == System.Formats.Tar.TarEntryType.RegularFile)
            {
                // 안전하게 경로 생성 (../ 방지)
                var safeName = Path.GetFileName(entry.Name);
                var filePath = Path.Combine(tempDir, safeName);

                using var outStream = File.Create(filePath);
                await entry.DataStream.CopyToAsync(outStream);
            }
        }

        return Results.Ok("Upload and extraction complete");
    }
    catch (Exception ex)
    {
        return Results.StatusCode(500, $"Error processing upload: {ex.Message}");
    }
});

app.Run();
