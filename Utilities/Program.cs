using Amazon.S3;
using Amazon.S3.Model;
using CommandLine;
using Newtonsoft.Json;
using Serilog;  
using Utilities;

var config = new Dictionary<string, string>();
AmazonS3Client client;
Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateLogger();
var Logger = Log.ForContext<Program>();

Parser.Default.ParseArguments<UploadOptions, CreateIndexOptions, ListOptions, MapOptions>(args)
  .WithParsed<UploadOptions>(opts => { SetupEnv(opts); UploadFiles(opts).Wait(); })
  .WithParsed<CreateIndexOptions>(opts => { SetupEnv(opts); CreateIndex(opts).Wait(); })
  .WithParsed<ListOptions>(opts => { SetupEnv(opts); ListFiles(opts).Wait(); })
  .WithParsed<MapOptions>(opts => { SetupEnv(opts); MapHelper.Run(opts).Wait(); });


void SetupEnv(BaseOptions opts)
{
  if (opts.WorkDir != ".") {
    Environment.CurrentDirectory = Path.GetFullPath(opts.WorkDir);
  }

  var configFile = opts.Config;
  if (string.IsNullOrEmpty(configFile)) {
    Logger.Information("Config file is not set.");
    throw new Exception("Config file is not set.");
  }
  if (configFile.Contains("/") || configFile.Contains("\\")) {
    configFile = Path.GetFullPath(configFile);
  } else {
    configFile = FindConfig(configFile);
  }
  if (!File.Exists(configFile)) {
    Logger.Information($"Config file {configFile} does not exist.");
    throw new Exception($"Config file {configFile} does not exist.");
  }
  var raw = File.ReadAllText(configFile);
  config = JsonConvert.DeserializeObject<Dictionary<string, string>>(raw);
  if (config == null) {
    Logger.Information($"Config file {configFile} is not valid JSON.");
    throw new Exception($"Config file {configFile} is not valid JSON.");
  }
  client = new AmazonS3Client(config["AccessKeyId"], config["AccessKeySecret"], new AmazonS3Config
  {
    ServiceURL = config["Endpoint"],
    ForcePathStyle = true,
  });
}

async Task UploadFiles(UploadOptions opts)
{
  var existingFiles = await GetListFiles(opts.Prefix);
  var fileList = new List<string>();
  foreach (var file in opts.Files) {
    if (Directory.Exists(file)) {
      var dir = new DirectoryInfo(file);
      var files = dir.GetFiles("*", SearchOption.TopDirectoryOnly);
      foreach (var f in files) {
        fileList.Add(f.FullName);
      }
    } else
    if (File.Exists(file)) {
      fileList.Add(file);
    } else {
      Logger.Information($"File {file} does not exist.");
      return;
    }
  }
  int uploaded = 0;
  int skipped = 0;
  Logger.Information($"Uploading {fileList.Count} files to {config!["Bucket"]}/{opts.Prefix}...");
  foreach (var file in fileList) {
    if (!File.Exists(file)) {
      Logger.Information($"File {file} does not exist.");
      return;
    }
    var fileName = Path.GetFileName(file);
    var dest = $"{opts.Prefix}/{fileName}";
    if (!opts.Overwrite && existingFiles.Contains(dest)) {
      skipped++;
      continue;
    }
    await UploadFile(file, dest);
    uploaded++;
  }
  Logger.Information($"Uploaded {uploaded} files to {config!["Bucket"]}/{opts.Prefix}, skipped {skipped}");
}

async Task ListFiles(ListOptions opts)
{
  var existingFiles = await GetListFiles(opts.Prefix);
  Logger.Information($"Files in {opts.Prefix}:");
  foreach (var file in existingFiles) {
    Logger.Information(file);
  }
}

async Task CreateIndex(CreateIndexOptions opts)
{
  var existingFiles = await GetListFiles(opts.Prefix);
  var indexFile = Path.Combine(opts.WorkDir, "index.txt");
  using (var writer = new StreamWriter(indexFile)) {
    foreach (var file in existingFiles) {
      writer.WriteLine(file);
    }
  }
  await UploadFile(indexFile, $"{opts.Prefix}/index.txt");
  Logger.Information($"Uploaded index file to {config!["Bucket"]}/{opts.Prefix}/index.txt");
}

async Task<List<string>> GetListFiles(string prefix)
{
  Logger.Information("Getting list of files...");
  var list = new List<string>();
  string token = "";
  while (true) {
    var existingFilesResp = await client.ListObjectsV2Async(new Amazon.S3.Model.ListObjectsV2Request
    {
      BucketName = config!["Bucket"],
      Prefix = prefix,
      ContinuationToken = token == "" ? null : token
    });
    var existingFiles = existingFilesResp.S3Objects?.Select(o => o.Key).ToList() ?? [];
    list.AddRange(existingFiles);
    Logger.Information($"Got {existingFiles.Count} files, token: {existingFilesResp.NextContinuationToken}");
    if (existingFilesResp.IsTruncated ?? false) {
      token = existingFilesResp.NextContinuationToken;
    } else {
      break;
    }
  }
  Logger.Information($"Got {list.Count} files");
  return list;
}

string FindConfig(string filename)
{
  var dir = Directory.GetCurrentDirectory();
  while (true) {
    var configPath = Path.Combine(dir, filename);
    if (File.Exists(configPath)) {
      return configPath;
    }
    dir = Path.GetDirectoryName(dir);
    if (dir == null || dir == Path.GetPathRoot(dir)) {
      break;
    }
  }
  return "";
};

async Task UploadFile(string file, string path)
{
  var req = new PutObjectRequest()
  {
    BucketName = config!["Bucket"],
    Key = $"{path}",
    FilePath = file,
    ContentType = "application/zip",
    CannedACL = S3CannedACL.PublicRead,
  };
  var response = await client.PutObjectAsync(req);
  var filename = Path.GetFileName(file);
  Logger.Information($"Uploaded {filename} to {config!["Bucket"]}/{path}  Resp: {response.HttpStatusCode}");
}

class BaseOptions
{
  [Option('c', "config", Required = false, HelpText = "Path to the config file.", Default = "utilities.config.json")]
  public string Config { get; set; } = "utilities.config.json";

  [Option('w', "workdir", Required = false, HelpText = "Working directory.", Default = ".")]
  public string WorkDir { get; set; } = ".";
}

[Verb("upload", HelpText = "Upload files to S3.")]
class UploadOptions : BaseOptions
{
  [Option(Default = "railroader-mods")]
  public string Prefix { get; set; } = "railroader-mods";

  [Option(Default = false)]
  public bool Overwrite { get; set; }

  [Value(0)]
  public IEnumerable<string> Files { get; set; } = [];
}

[Verb("create-index", HelpText = "Generate index for files")]
class CreateIndexOptions : BaseOptions
{
  [Option(Default = "railroader-mods")]
  public string Prefix { get; set; } = "railroader-mods";
}

[Verb("list", HelpText = "List files in S3.")]
class ListOptions : BaseOptions
{

  [Option(Default = "railroader-mods")]
  public string Prefix { get; set; } = "railroader-mods";
}
