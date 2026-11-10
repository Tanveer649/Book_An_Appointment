using System.Globalization;
using System.Text;
using System.Threading.Channels;

namespace Book_An_Appointment1.Helpers
{
    // Daily log files: {Path}/portal-yyyyMMdd.log (appsettings "FileLogging").
    // Only the last RetainDays days are kept — older files are deleted at start-up and every time a new day's file
    // is started (RetainDays 5 = today + the 4 days before). Levels follow appsettings "Logging:LogLevel".
    // Lines are queued and written by one background task, so logging never slows down a request.
    public sealed class FileLoggingOptions
    {
        public bool Enabled { get; set; } = true;
        public string Path { get; set; } = "logs";          // relative to the site folder, or an absolute path
        public int RetainDays { get; set; } = 5;
    }

    [ProviderAlias("File")]
    public sealed class FileLoggerProvider : ILoggerProvider
    {
        private const string FilePrefix = "portal-";
        private const string FileSuffix = ".log";

        private readonly string _folder;
        private readonly int _retainDays;
        private readonly Channel<string> _queue = Channel.CreateUnbounded<string>(new UnboundedChannelOptions { SingleReader = true });
        private readonly Task _writer;
        private StreamWriter? _stream;
        private DateTime _streamDay;

        public FileLoggerProvider(string folder, int retainDays)
        {
            _folder = folder;
            _retainDays = Math.Clamp(retainDays, 1, 365);
            Directory.CreateDirectory(_folder);
            DeleteOldFiles();
            _writer = Task.Run(WriteLoopAsync);
        }

        public ILogger CreateLogger(string categoryName) => new FileLogger(categoryName, this);

        internal void Enqueue(string line) => _queue.Writer.TryWrite(line);

        private async Task WriteLoopAsync()
        {
            var reader = _queue.Reader;
            while (await reader.WaitToReadAsync())
            {
                while (reader.TryRead(out var line))
                {
                    try
                    {
                        var today = DateTime.Today;
                        if (_stream == null || _streamDay != today)
                        {
                            _stream?.Dispose();
                            _stream = new StreamWriter(new FileStream(System.IO.Path.Combine(_folder, FileName(today)),
                                FileMode.Append, FileAccess.Write, FileShare.ReadWrite), new UTF8Encoding(false));
                            _streamDay = today;
                            DeleteOldFiles();                  // new day → drop the files that are now too old
                        }
                        _stream.WriteLine(line);
                    }
                    catch (IOException) { /* disk full / locked — never break the app because of logging */ }
                    catch (UnauthorizedAccessException) { }
                }
                try { _stream?.Flush(); } catch (IOException) { }
            }
        }

        // Keeps today + the (RetainDays - 1) days before it
        private void DeleteOldFiles()
        {
            var oldestKept = DateTime.Today.AddDays(-(_retainDays - 1));
            try
            {
                foreach (var file in Directory.EnumerateFiles(_folder, FilePrefix + "*" + FileSuffix))
                {
                    var stamp = System.IO.Path.GetFileNameWithoutExtension(file)[FilePrefix.Length..];
                    if (DateTime.TryParseExact(stamp, "yyyyMMdd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var day) && day < oldestKept)
                        try { File.Delete(file); } catch (IOException) { } catch (UnauthorizedAccessException) { }
                }
            }
            catch (IOException) { }
            catch (UnauthorizedAccessException) { }
        }

        private static string FileName(DateTime day) => FilePrefix + day.ToString("yyyyMMdd", CultureInfo.InvariantCulture) + FileSuffix;

        public void Dispose()
        {
            _queue.Writer.TryComplete();
            try { _writer.Wait(TimeSpan.FromSeconds(3)); } catch (AggregateException) { }
            _stream?.Dispose();
        }

        private sealed class FileLogger : ILogger
        {
            private readonly string _category;
            private readonly FileLoggerProvider _provider;

            public FileLogger(string category, FileLoggerProvider provider)
            {
                _category = category;
                _provider = provider;
            }

            public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

            // Minimum levels come from appsettings Logging:LogLevel (applied by the logging framework)
            public bool IsEnabled(LogLevel logLevel) => logLevel != LogLevel.None;

            public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception,
                Func<TState, Exception?, string> formatter)
            {
                if (!IsEnabled(logLevel)) return;
                var sb = new StringBuilder()
                    .Append(DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff", CultureInfo.InvariantCulture))
                    .Append(" [").Append(Short(logLevel)).Append("] ")
                    .Append(_category).Append(": ")
                    .Append(formatter(state, exception));
                if (exception != null) sb.AppendLine().Append(exception);
                _provider.Enqueue(sb.ToString());
            }

            private static string Short(LogLevel level) => level switch
            {
                LogLevel.Trace => "TRC",
                LogLevel.Debug => "DBG",
                LogLevel.Information => "INF",
                LogLevel.Warning => "WRN",
                LogLevel.Error => "ERR",
                LogLevel.Critical => "CRT",
                _ => "---"
            };
        }
    }

    public static class FileLoggingExtensions
    {
        // appsettings "FileLogging": { "Enabled": true, "Path": "logs", "RetainDays": 5 }
        public static ILoggingBuilder AddPortalFileLogging(this ILoggingBuilder logging, IConfiguration configuration, IHostEnvironment environment)
        {
            var options = configuration.GetSection("FileLogging").Get<FileLoggingOptions>() ?? new FileLoggingOptions();
            if (!options.Enabled) return logging;

            var folder = System.IO.Path.IsPathRooted(options.Path)
                ? options.Path
                : System.IO.Path.Combine(environment.ContentRootPath, string.IsNullOrWhiteSpace(options.Path) ? "logs" : options.Path);
            try
            {
                logging.AddProvider(new FileLoggerProvider(folder, options.RetainDays));
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                // Folder not writable (IIS permissions): the portal still runs, only without log files
                Console.Error.WriteLine($"File logging disabled — cannot write to '{folder}': {ex.Message}");
            }
            return logging;
        }
    }
}
